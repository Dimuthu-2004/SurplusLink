using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Routing;
using SurplusLink.Api.Transactions;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class PreS12GoldenWorkflowTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [LiveWorkflowFact]
    public async Task Golden_http_lifecycle_runs_real_graph_and_persists_participant_outcomes()
    {
        using var app = fixture.App().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.PostConfigure<WorkflowExecutionOptions>(options =>
            {
                options.Enabled = true;
                options.PollSeconds = 1;
                options.BaseUrl = Environment.GetEnvironmentVariable("SURPLUSLINK_AI_TEST_URL")!;
                options.SharedToken = Environment.GetEnvironmentVariable("AI_SERVICE_SHARED_TOKEN")!;
            });
            services.AddSingleton<ITransportEstimateService, TestTransport>();
            services.AddHostedService<WorkflowExecutionWorker>();
        }));
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER", "SELLER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        foreach (var decision in new[] { "approve", "reject", "revise" })
        {
            var categoryResponse = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Floor Tiles " + Guid.NewGuid() });
            categoryResponse.EnsureSuccessStatusCode();
            var category = (await categoryResponse.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
            var listingIds = new List<Guid>();
            foreach (var (quantity, price) in new[] { (500, 800), (200, 700) })
            {
                var create = await seller.PostAsJsonAsync("/api/materials", new { categoryId = category.Id,
                    title = "Floor Tiles", description = "Golden audit stock", quantity, unit = "M2", condition = "GOOD",
                    unitPrice = price, latitude = 6.8, longitude = 79.9, availableUntil = DateTime.UtcNow.AddDays(30) });
                create.EnsureSuccessStatusCode();
                var listing = (await create.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
                listingIds.Add(listing.Id);
                (await seller.PatchAsync($"/api/materials/{listing.Id}/publish", null)).EnsureSuccessStatusCode();
                (await manager.PatchAsJsonAsync($"/api/materials/{listing.Id}/verify", new { approved = true })).EnsureSuccessStatusCode();
            }
            var created = await buyer.PostAsJsonAsync("/api/requirements", new { categoryId = category.Id,
                requiredQuantity = 400, unit = "m2", maximumBudget = 400000,
                deadline = DateTime.UtcNow.AddDays(7), latitude = 6.9, longitude = 79.8 });
            created.EnsureSuccessStatusCode();
            var request = (await created.Content.ReadFromJsonAsync<RequirementResponse>())!;
            (await buyer.PostAsync($"/api/requirements/{request.Id}/submit", null)).EnsureSuccessStatusCode();
            var started = await buyer.PostAsync($"/api/requirements/{request.Id}/start-matching", null);
            started.EnsureSuccessStatusCode();
            var start = (await started.Content.ReadFromJsonAsync<StartMatchingResponse>())!;
            Assert.Equal(BuyerRequestStatus.MATCHING, start.Requirement.Status);
            await WaitForMatch(buyer, request.Id);
            using (var db = fixture.Context())
            {
                var workflow = await db.AgentWorkflows.SingleAsync(x => x.Id == start.WorkflowId);
                Assert.Equal(1, await db.AgentWorkflows.CountAsync(x => x.MaterialRequestId == request.Id));
                Assert.True(workflow.Status == AgentWorkflowStatus.PENDING_APPROVAL, workflow.ErrorJson ?? workflow.ValidationJson);
                Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == request.Id));
                Assert.Equal(0, await db.Listings.Where(x => listingIds.Contains(x.Id)).SumAsync(x => x.ReservedQuantity));
            }
            var matches = await buyer.GetFromJsonAsync<MatchPage>($"/api/matches/requirement/{request.Id}");
            Assert.Equal(2, matches!.Total);
            Assert.Contains(matches.Items, x => x.ListingId == listingIds[1] && x.RejectionReason == "INSUFFICIENT_QUANTITY");
            Assert.Contains(matches.Items, x => x.ListingId == listingIds[0] && x.DurationMinutes == 30);
            var pending = await manager.GetFromJsonAsync<JsonElement>("/api/workflows?status=PENDING_APPROVAL");
            Assert.Contains(pending.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == start.WorkflowId);
            (await manager.PostAsJsonAsync($"/api/workflows/{start.WorkflowId}/{decision}", new { note = "Golden audit " + decision })).EnsureSuccessStatusCode();
            using (var db = fixture.Context())
            {
                Assert.Equal(decision == "approve" ? 400m : 0m, (await db.Listings.FindAsync(listingIds[0]))!.ReservedQuantity);
                var transaction = await db.Transactions.Include(x => x.Offer).SingleAsync(x => x.Offer.MaterialMatch.MaterialRequestId == request.Id);
                Assert.Equal(decision == "approve" ? TransactionStatus.APPROVED : TransactionStatus.REJECTED, transaction.Status);
                Assert.Equal(decision == "approve" ? 400m : 0m, transaction.ReservedQuantity);
                Assert.Equal(decision == "revise" ? OfferStatus.REVISION_REQUESTED : decision == "approve" ? OfferStatus.ACCEPTED : OfferStatus.REJECTED, transaction.Offer.Status);
                Assert.True((await seller.GetAsync($"/api/transactions/{transaction.Id}/history")).IsSuccessStatusCode);
                Assert.True((await buyer.GetAsync($"/api/transactions/{transaction.Id}/history")).IsSuccessStatusCode);
                if (decision == "approve")
                {
                    (await manager.PostAsync($"/api/transactions/{transaction.Id}/approve", null)).EnsureSuccessStatusCode();
                    await db.Entry(await db.Listings.SingleAsync(x => x.Id == listingIds[0])).ReloadAsync();
                    Assert.Equal(400m, (await db.Listings.FindAsync(listingIds[0]))!.ReservedQuantity);
                }
                Assert.Equal("Golden audit " + decision, (await db.Approvals.SingleAsync(x => x.AgentWorkflowId == start.WorkflowId)).Note);
            }
            if (decision == "revise")
            {
                var restart = await buyer.PostAsync($"/api/requirements/{request.Id}/start-matching", null);
                restart.EnsureSuccessStatusCode();
                var resumed = (await restart.Content.ReadFromJsonAsync<StartMatchingResponse>())!;
                Assert.NotEqual(start.WorkflowId, resumed.WorkflowId);
                await WaitForMatch(buyer, request.Id);
                using var db = fixture.Context();
                Assert.Equal(AgentWorkflowStatus.PENDING_APPROVAL, (await db.AgentWorkflows.FindAsync(resumed.WorkflowId))!.Status);
                Assert.Equal(0m, (await db.Listings.FindAsync(listingIds[0]))!.ReservedQuantity);
                (await manager.PostAsJsonAsync($"/api/workflows/{resumed.WorkflowId}/approve", new { note = "Revised review" })).EnsureSuccessStatusCode();
                var approvedTransaction = await db.Transactions.AsNoTracking().SingleAsync(x =>
                    x.Offer.MaterialMatch.MaterialRequestId == request.Id && x.Status == TransactionStatus.APPROVED);
                Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync($"/api/transactions/{approvedTransaction.Id}/complete", null)).StatusCode);
                (await seller.PostAsync($"/api/transactions/{approvedTransaction.Id}/handover", null)).EnsureSuccessStatusCode();
                Assert.Equal(BuyerRequestStatus.APPROVED, (await buyer.GetFromJsonAsync<RequirementResponse>($"/api/requirements/{request.Id}"))!.Status);
                (await buyer.PostAsync($"/api/transactions/{approvedTransaction.Id}/confirm-receipt", null)).EnsureSuccessStatusCode();
                var finalRequest = await buyer.GetFromJsonAsync<RequirementResponse>($"/api/requirements/{request.Id}");
                Assert.Equal(BuyerRequestStatus.COMPLETED, finalRequest!.Status);
                var sellerOutcome = await seller.GetFromJsonAsync<TransactionResponse>($"/api/transactions/{approvedTransaction.Id}");
                Assert.Equal(TransactionStatus.COMPLETED, sellerOutcome!.Status);
                Assert.Equal(400m, sellerOutcome.ReservedQuantity);
                Assert.Equal(2, await db.AgentWorkflows.CountAsync(x => x.MaterialRequestId == request.Id));
                Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == request.Id));
            }
        }
    }

    private static async Task WaitForMatch(HttpClient buyer, Guid id)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var row = await buyer.GetFromJsonAsync<RequirementResponse>($"/api/requirements/{id}");
            if (row!.Status != BuyerRequestStatus.MATCHING)
            {
                Assert.Equal(BuyerRequestStatus.MATCH_FOUND, row.Status);
                return;
            }
            await Task.Delay(200);
        }
        Assert.Fail("Hosted worker did not finish the queued workflow.");
    }

    private sealed class TestTransport : ITransportEstimateService
    {
        public Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken ct) =>
            Task.FromResult(new TransportEstimate(new RouteResult(10, 30), 500));
    }
}
