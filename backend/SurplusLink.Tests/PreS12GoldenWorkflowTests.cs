using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        using var app = fixture.App();
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
            using (var db = fixture.Context())
            {
                var options = Options.Create(new WorkflowExecutionOptions { BaseUrl = Environment.GetEnvironmentVariable("SURPLUSLINK_AI_TEST_URL")!,
                    SharedToken = Environment.GetEnvironmentVariable("AI_SERVICE_SHARED_TOKEN")! });
                using var http = new HttpClient();
                Assert.True(await new WorkflowQueueProcessor(db, new AgentWorkflowClient(http, options), new TestTransport(), options).ProcessNextAsync(default));
                var workflow = await db.AgentWorkflows.SingleAsync(x => x.Id == start.WorkflowId);
                Assert.True(workflow.Status == AgentWorkflowStatus.PENDING_APPROVAL, workflow.ErrorJson ?? workflow.ValidationJson);
                Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == request.Id));
                Assert.Equal(0, await db.Listings.Where(x => listingIds.Contains(x.Id)).SumAsync(x => x.ReservedQuantity));
            }
            var matches = await buyer.GetFromJsonAsync<MatchPage>($"/api/matches/requirement/{request.Id}");
            Assert.Equal(2, matches!.Total);
            Assert.Contains(matches.Items, x => x.ListingId == listingIds[1] && x.RejectionReason == "INSUFFICIENT_QUANTITY");
            Assert.Contains(matches.Items, x => x.ListingId == listingIds[0] && x.DurationMinutes == 30);
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
                using var db = fixture.Context();
                using var http = new HttpClient();
                var options = Options.Create(new WorkflowExecutionOptions { BaseUrl = Environment.GetEnvironmentVariable("SURPLUSLINK_AI_TEST_URL")!,
                    SharedToken = Environment.GetEnvironmentVariable("AI_SERVICE_SHARED_TOKEN")! });
                Assert.True(await new WorkflowQueueProcessor(db, new AgentWorkflowClient(http, options), new TestTransport(), options).ProcessNextAsync(default));
                Assert.Equal(AgentWorkflowStatus.PENDING_APPROVAL, (await db.AgentWorkflows.FindAsync(resumed.WorkflowId))!.Status);
                Assert.Equal(0m, (await db.Listings.FindAsync(listingIds[0]))!.ReservedQuantity);
                (await manager.PostAsJsonAsync($"/api/workflows/{resumed.WorkflowId}/approve", new { note = "Revised review" })).EnsureSuccessStatusCode();
                var approvedTransaction = await db.Transactions.AsNoTracking().SingleAsync(x =>
                    x.Offer.MaterialMatch.MaterialRequestId == request.Id && x.Status == TransactionStatus.APPROVED);
                (await manager.PostAsync($"/api/transactions/{approvedTransaction.Id}/complete", null)).EnsureSuccessStatusCode();
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

    private sealed class TestTransport : ITransportEstimateService
    {
        public Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken ct) =>
            Task.FromResult(new TransportEstimate(new RouteResult(10, 30), 500));
    }
}
