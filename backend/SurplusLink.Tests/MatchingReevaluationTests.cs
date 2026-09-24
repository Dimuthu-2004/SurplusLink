using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Routing;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class MatchingReevaluationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Both_verification_orders_reuse_candidates_and_publish_routes_to_buyer_and_manager()
    {
        foreach (var verifyFirst in new[] { false, true })
        {
            using var app = fixture.App();
            using var seller = fixture.Client(app, fixture.Seller, "SELLER");
            using var buyer = fixture.Client(app, fixture.Buyer);
            using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
            var categoryResponse = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Tiles " + Guid.NewGuid(), allowedUnits = new[] { "pcs" } });
            categoryResponse.EnsureSuccessStatusCode();
            var category = (await categoryResponse.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
            var created = await seller.PostAsJsonAsync("/api/materials", new {
                categoryId = category.Id, title = "Tiles", description = "Integration stock", quantity = 500,
                unit = "pcs", unitPrice = 800, condition = "GOOD", availableUntil = DateTime.UtcNow.AddDays(30),
                latitude = 6.8m, longitude = 79.9m
            });
            created.EnsureSuccessStatusCode();
            var listing = (await created.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
            (await seller.PatchAsync($"/api/materials/{listing.Id}/publish", null)).EnsureSuccessStatusCode();
            if (verifyFirst) (await manager.PatchAsJsonAsync($"/api/materials/{listing.Id}/verify", new { approved = true })).EnsureSuccessStatusCode();
            var saved = await buyer.PostAsJsonAsync("/api/requirements", new {
                categoryId = category.Id, requiredQuantity = 400, unit = "pcs", maximumBudget = 400000,
                deadline = DateTime.UtcNow.AddDays(6), latitude = 6.9m, longitude = 79.8m
            });
            saved.EnsureSuccessStatusCode();
            var request = (await saved.Content.ReadFromJsonAsync<RequirementResponse>())!;
            (await buyer.PostAsync($"/api/requirements/{request.Id}/submit", null)).EnsureSuccessStatusCode();
            var path = $"/api/matches/requirement/{request.Id}";
            (await buyer.PostAsync(path + "/generate", null)).EnsureSuccessStatusCode();
            var original = Assert.Single((await buyer.GetFromJsonAsync<MatchPage>(path))!.Items);
            Assert.Equal(verifyFirst ? "GENERATED" : "REJECTED", original.Status);
            if (!verifyFirst)
            {
                Assert.Equal("LISTING_NOT_ACTIVE", original.RejectionReason);
                (await buyer.PostAsync($"/api/requirements/{request.Id}/start-matching", null)).EnsureSuccessStatusCode();
                using (var db = fixture.Context())
                    await new WorkflowQueueProcessor(db, new DeterministicClient(), new CheckedTransport(), Options.Create(new WorkflowExecutionOptions())).ProcessNextAsync(default);
                (await manager.PatchAsJsonAsync($"/api/materials/{listing.Id}/verify", new { approved = true })).EnsureSuccessStatusCode();
                (await buyer.PostAsync(path + "/generate", null)).EnsureSuccessStatusCode();
                var reevaluated = Assert.Single((await buyer.GetFromJsonAsync<MatchPage>(path))!.Items);
                Assert.Equal(original.Id, reevaluated.Id);
                Assert.True(reevaluated.Valid);
                Assert.Null(reevaluated.RejectionReason);
            }
            (await buyer.PostAsync($"/api/requirements/{request.Id}/start-matching", null)).EnsureSuccessStatusCode();
            using (var db = fixture.Context())
                await new WorkflowQueueProcessor(db, new DeterministicClient(), new CheckedTransport(), Options.Create(new WorkflowExecutionOptions())).ProcessNextAsync(default);
            foreach (var reader in new[] { buyer, manager })
            {
                var candidate = Assert.Single((await reader.GetFromJsonAsync<MatchPage>(path))!.Items);
                Assert.Equal(original.Id, candidate.Id);
                Assert.Equal("ROUTED", candidate.Status);
                Assert.True(candidate.Valid);
                Assert.Equal("Tiles", candidate.MaterialTitle);
                Assert.Equal(400, candidate.Quantity); Assert.Equal(500, candidate.AvailableQuantity);
                Assert.Equal(800, candidate.UnitPrice);
                Assert.Equal(12.5m, candidate.Distance); Assert.Equal(30, candidate.DurationMinutes);
                Assert.Equal(785, candidate.EstimatedTransportCost);
            }
            using var verify = fixture.Context();
            Assert.Single(await verify.Matches.Where(x => x.MaterialRequestId == request.Id).ToListAsync());
            Assert.False(await verify.Reservations.AnyAsync(x => x.MaterialRequestId == request.Id));
            if (!verifyFirst)
            {
                Assert.True(await verify.AuditLogs.AnyAsync(x => x.EntityId == original.Id && x.Action == "STALE_LISTING_VERIFIED"));
                Assert.True(await verify.AuditLogs.AnyAsync(x => x.EntityId == original.Id && x.Action == "REEVALUATE"));
            }
            var summary = (await manager.GetFromJsonAsync<RequirementAnalyticsSummary>("/api/requirements/analytics/summary?upcomingDays=7"))!;
            Assert.Contains(summary.UpcomingDeadlines, x => x.Id == request.Id);
            Assert.DoesNotContain(summary.UpcomingDeadlines, x => x.Id == listing.Id);
            // An approval recommendation is protected even before reservation.
            Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/generate", null)).StatusCode);
        }
    }

    [PostgresFact]
    public async Task Reevaluate_clears_stale_metrics_but_retains_deterministic_rejections_and_final_outcomes()
    {
        using var db = fixture.Context();
        var category = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var request = new BuyerRequest { Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category,
            RequiredQuantity = 400, MaximumBudget = 400000, Unit = "pcs", Deadline = DateTime.UtcNow.AddDays(6), Status = BuyerRequestStatus.OPEN };
        var listing = new Listing { Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category, Title = "Tiles",
            Quantity = 500, Unit = "pcs", UnitPrice = 800, Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(30) };
        db.AddRange(request, listing); await db.SaveChangesAsync();
        var service = new MatchService(db);
        var first = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        await service.RankAsync(first.Id, .9m, fixture.Manager, default);
        await service.RecordRouteAsync(first.Id, true, 12, 500, fixture.Manager, default);
        first.DurationMinutes = 30; listing.Quantity = 200; await db.SaveChangesAsync();
        var second = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        Assert.Equal(first.Id, second.Id); Assert.Equal("INSUFFICIENT_QUANTITY", second.RejectionReason);
        Assert.Equal(0, second.Score); Assert.Null(second.Distance); Assert.Null(second.DurationMinutes); Assert.Null(second.EstimatedTransportCost);
        Assert.Equal("INSUFFICIENT_QUANTITY", (await service.GenerateAsync(request.Id, listing.Id, null, default)).RejectionReason);
        listing.Quantity = 500; listing.SellerId = fixture.Buyer; await db.SaveChangesAsync();
        Assert.Equal("SELF_MATCH_NOT_ALLOWED", (await service.GenerateAsync(request.Id, listing.Id, null, default)).RejectionReason);
        listing.SellerId = fixture.Seller; await db.SaveChangesAsync();
        await service.GenerateAsync(request.Id, listing.Id, null, default);
        await service.RecordRouteAsync(first.Id, true, 12, 500, null, default);
        request.Status = BuyerRequestStatus.APPROVED;
        var reservation = new Reservation { Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Quantity = 400, Status = ReservationStatus.ACTIVE };
        db.Reservations.Add(reservation); listing.ReservedQuantity = 400; await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<MatchException>(() => service.GenerateAsync(request.Id, listing.Id, null, default))).StatusCode);
        Assert.Equal(MatchStatus.ROUTED, first.Status); Assert.Equal(12, first.Distance); Assert.Equal(400, reservation.Quantity);
        Assert.Equal(1, await db.Matches.CountAsync(x => x.MaterialRequestId == request.Id));
    }

    [PostgresFact]
    public async Task Concurrent_generation_reuses_unique_pair_and_reservations_protect_even_open_requests()
    {
        Guid requestId, listingId;
        using (var seed = fixture.Context())
        {
            var category = Guid.Parse("00000000-0000-0000-0000-000000000101");
            var request = new BuyerRequest { Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category,
                RequiredQuantity = 1, MaximumBudget = 1000, Unit = "pcs", Deadline = DateTime.UtcNow.AddDays(6), Status = BuyerRequestStatus.OPEN };
            var listing = new Listing { Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category, Title = "Tiles",
                Quantity = 500, Unit = "pcs", UnitPrice = 800, Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(30) };
            seed.AddRange(request, listing); await seed.SaveChangesAsync(); requestId = request.Id; listingId = listing.Id;
        }
        async Task<Guid> Generate()
        {
            using var db = fixture.Context();
            return (await new MatchService(db).GenerateAsync(requestId, listingId, null, default)).Id;
        }
        var ids = await Task.WhenAll(Generate(), Generate());
        Assert.Equal(ids[0], ids[1]);
        using var verify = fixture.Context();
        Assert.Equal(1, await verify.Matches.CountAsync(x => x.MaterialRequestId == requestId));
        verify.Reservations.Add(new Reservation { Id = Guid.NewGuid(), ListingId = listingId,
            MaterialRequestId = requestId, Quantity = 1, Status = ReservationStatus.ACTIVE });
        await verify.SaveChangesAsync();
        var before = await verify.AuditLogs.CountAsync(x => x.EntityId == ids[0]);
        Assert.Equal(409, (await Assert.ThrowsAsync<MatchException>(() => Generate())).StatusCode);
        Assert.Equal(before, await verify.AuditLogs.CountAsync(x => x.EntityId == ids[0]));
    }

    private sealed class DeterministicClient : IAgentWorkflowClient
    {
        public Task<(WorkflowRunResult Result, int Retries)> RunAsync(WorkflowRunRequest request, CancellationToken ct) =>
            Task.FromResult((!request.Listings.Any(x => x.Status == "ACTIVE" && x.TransportCost is not null)
                ? new WorkflowRunResult(request.WorkflowId, "REVISION_REQUESTED", new(false, false, null, ["NO_CANDIDATES"], []), null, [], null)
                : WorkflowExecutionTests.Success(request), 0));
    }
    private sealed class CheckedTransport : ITransportEstimateService
    {
        public Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken ct)
        {
            Assert.Equal(6.8m, request.SellerLatitude); Assert.Equal(79.9m, request.SellerLongitude);
            Assert.Equal(6.9m, request.BuyerLatitude); Assert.Equal(79.8m, request.BuyerLongitude);
            return Task.FromResult(new TransportEstimate(new RouteResult(12.5m, 30), 785));
        }
    }
}
