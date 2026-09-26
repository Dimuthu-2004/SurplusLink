using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class AgentWorkflowIntegrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Concurrent_workflows_cannot_overreserve_shared_stock_and_expired_stock_is_blocked()
    {
        var first = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        var second = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        Guid listingId;
        using (var db = fixture.Context())
        {
            var a = await db.Matches.Include(x => x.Listing).SingleAsync(x => x.Id == first.MaterialMatchId);
            var b = await db.Matches.SingleAsync(x => x.Id == second.MaterialMatchId);
            a.Listing.Quantity = 3;
            b.ListingId = a.ListingId;
            listingId = a.ListingId;
            await db.SaveChangesAsync();
        }
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        var results = await Task.WhenAll(new[] { first, second }.Select(w =>
            manager.PostAsJsonAsync($"/api/workflows/{w.Id}/approve", new { note = "Concurrent review" })));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        using (var db = fixture.Context())
        {
            Assert.Equal(2m, (await db.Listings.FindAsync(listingId))!.ReservedQuantity);
            Assert.Equal(1, await db.Reservations.CountAsync(x => x.ListingId == listingId));
        }
        var expired = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using (var db = fixture.Context())
        {
            var listing = await db.Matches.Where(x => x.Id == expired.MaterialMatchId).Select(x => x.Listing).SingleAsync();
            listing.AvailableUntil = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync($"/api/workflows/{expired.Id}/approve", new { note = "Stale" })).StatusCode);
    }

    [PostgresFact]
    public async Task Manager_approval_reserves_once_and_decisions_are_audited()
    {
        var workflow = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync($"/api/workflows/{workflow.Id}")).StatusCode);
        var approved = await manager.PostAsJsonAsync($"/api/workflows/{workflow.Id}/approve", new { note = "Approved after validation." });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var response = (await approved.Content.ReadFromJsonAsync<AgentWorkflowResponse>())!;
        Assert.Equal(AgentWorkflowStatus.APPROVED, response.Status);
        Assert.Single(response.Approvals);

        using var db = fixture.Context();
        Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == workflow.MaterialRequestId));
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityId == workflow.Id).Select(x => x.Action).ToListAsync(),
            x => x == "WORKFLOW_APPROVED");
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PostAsJsonAsync($"/api/workflows/{workflow.Id}/approve", new { note = "Idempotent." })).StatusCode);
        Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == workflow.MaterialRequestId));
    }

    [PostgresFact]
    public async Task Reject_and_revise_never_reserve_and_store_manager_decisions()
    {
        var rejected = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        var revised = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        var reject = await manager.PostAsJsonAsync($"/api/workflows/{rejected.Id}/reject", new { note = "Validation failed." });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        var revise = await manager.PostAsJsonAsync($"/api/workflows/{revised.Id}/revise", new { note = "Correct the quantity." });
        Assert.Equal(HttpStatusCode.OK, revise.StatusCode);
        var revisedResponse = (await revise.Content.ReadFromJsonAsync<AgentWorkflowResponse>())!;
        Assert.Equal(AgentWorkflowStatus.REVISION_REQUESTED, revisedResponse.Status);
        Assert.Contains(revisedResponse.Steps, x => x.Stage == "REVISION");

        using var db = fixture.Context();
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == rejected.MaterialRequestId));
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == revised.MaterialRequestId));
        Assert.Equal(2, await db.Approvals.CountAsync(x => x.AgentWorkflowId == rejected.Id || x.AgentWorkflowId == revised.Id));
    }

    [PostgresFact]
    public async Task Buyer_and_seller_claims_never_grant_manager_workflow_decisions_but_dual_role_trade_with_another_party_is_valid()
    {
        var approve = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        var reject = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        var revise = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using var app = fixture.App();
        // Both identities carry both marketplace roles; neither carries MANAGER.
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER", "SELLER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER", "BUYER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await buyer.PostAsJsonAsync($"/api/workflows/{approve.Id}/approve", new { note = "No manager claim." })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await seller.PostAsJsonAsync($"/api/workflows/{reject.Id}/reject", new { note = "No manager claim." })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await buyer.PostAsJsonAsync($"/api/workflows/{revise.Id}/revise", new { note = "No manager claim." })).StatusCode);

        // The dual-role buyer and a distinct seller remain a legitimate pairing;
        // only the independent manager may authorize its reservation.
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PostAsJsonAsync($"/api/workflows/{approve.Id}/approve", new { note = "Different counterparties." })).StatusCode);
        using var db = fixture.Context();
        Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == approve.MaterialRequestId));
        Assert.All(await db.AgentWorkflows.Where(x => x.Id == reject.Id || x.Id == revise.Id).ToListAsync(),
            row => Assert.Equal(AgentWorkflowStatus.PENDING_APPROVAL, row.Status));
    }

    [PostgresFact]
    public async Task Approval_rechecks_transport_budget_delivery_and_route_completeness()
    {
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        foreach (var defect in new[] { "budget", "route", "travelTime" })
        {
            var workflow = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
            using (var db = fixture.Context())
            {
                var match = await db.Matches.Include(x => x.Listing).SingleAsync(x => x.Id == workflow.MaterialMatchId);
                if (defect == "budget") match.EstimatedTransportCost = 990;
                if (defect == "route") match.DurationMinutes = null;
                if (defect == "travelTime") match.DurationMinutes = 10000;
                await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.Conflict,
                (await manager.PostAsJsonAsync($"/api/workflows/{workflow.Id}/approve", new { note = defect })).StatusCode);
            using var verify = fixture.Context();
            Assert.False(await verify.Reservations.AnyAsync(x => x.MaterialRequestId == workflow.MaterialRequestId));
            Assert.Equal(AgentWorkflowStatus.PENDING_APPROVAL, (await verify.AgentWorkflows.FindAsync(workflow.Id))!.Status);
        }
    }

    [PostgresFact]
    public async Task Multi_match_selection_approval_reserves_atomically_and_shortage_rolls_back()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        Guid reqId, listingAId, listingBId, matchAId, matchBId;
        using (var db = fixture.Context())
        {
            var category = await db.Categories.FirstAsync();
            var request = new BuyerRequest
            {
                Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Multi-match test",
                RequiredQuantity = 50, MaximumBudget = 2000, Unit = "pcs", Deadline = DateTime.UtcNow.AddDays(7),
                Status = BuyerRequestStatus.MATCH_FOUND
            };
            var listingA = new Listing
            {
                Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Seller A stock",
                Quantity = 20, ReservedQuantity = 0, Unit = "pcs", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(10),
                Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
            };
            var listingB = new Listing
            {
                Id = Guid.NewGuid(), SellerId = fixture.OtherBuyer, CategoryId = category.Id, Title = "Seller B stock",
                Quantity = 30, ReservedQuantity = 0, Unit = "pcs", UnitPrice = 12, AvailableUntil = DateTime.UtcNow.AddDays(10),
                Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
            };
            var matchA = new MaterialMatch
            {
                Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listingA.Id,
                Status = MatchStatus.ROUTED, Distance = 10, DurationMinutes = 30, EstimatedTransportCost = 50
            };
            var matchB = new MaterialMatch
            {
                Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listingB.Id,
                Status = MatchStatus.ROUTED, Distance = 15, DurationMinutes = 45, EstimatedTransportCost = 60
            };
            db.AddRange(request, listingA, listingB, matchA, matchB);
            await db.SaveChangesAsync();
            reqId = request.Id;
            listingAId = listingA.Id;
            listingBId = listingB.Id;
            matchAId = matchA.Id;
            matchBId = matchB.Id;
        }

        // 1. Over-allocation rejected: 20 + 35 = 55 > 50
        var overSelect = await buyer.PostAsJsonAsync($"/api/requirements/{reqId}/select-matches", new
        {
            allocations = new[]
            {
                new { matchId = matchAId, quantity = 20m },
                new { matchId = matchBId, quantity = 35m }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, overSelect.StatusCode);

        // 2. Exceeding single seller available stock rejected: Seller A has 20, asking 25
        var exceedStock = await buyer.PostAsJsonAsync($"/api/requirements/{reqId}/select-matches", new
        {
            allocations = new[]
            {
                new { matchId = matchAId, quantity = 25m },
                new { matchId = matchBId, quantity = 25m }
            }
        });
        Assert.Equal(HttpStatusCode.Conflict, exceedStock.StatusCode);

        // 3. Valid multi-match selection: 20 from A, 30 from B = 50 total
        var selectOk = await buyer.PostAsJsonAsync($"/api/requirements/{reqId}/select-matches", new
        {
            allocations = new[]
            {
                new { matchId = matchAId, quantity = 20m },
                new { matchId = matchBId, quantity = 30m }
            }
        });
        Assert.Equal(HttpStatusCode.OK, selectOk.StatusCode);

        // Verify stock is NOT reserved yet!
        using (var db = fixture.Context())
        {
            Assert.Equal(0, (await db.Listings.FindAsync(listingAId))!.ReservedQuantity);
            Assert.Equal(0, (await db.Listings.FindAsync(listingBId))!.ReservedQuantity);
            Assert.Empty(await db.Reservations.Where(x => x.MaterialRequestId == reqId).ToListAsync());
            var req = await db.BuyerRequests.FindAsync(reqId);
            Assert.Equal(BuyerRequestStatus.PENDING_APPROVAL, req!.Status);
            var txs = await db.Transactions.Where(x => x.Offer.MaterialMatch.MaterialRequestId == reqId).ToListAsync();
            Assert.Equal(2, txs.Count);
            Assert.All(txs, t => Assert.Equal(0, t.ReservedQuantity));
        }

        // 4. Manager approval reserves atomically across both listings
        Guid workflowId;
        using (var db = fixture.Context())
        {
            workflowId = (await db.AgentWorkflows.SingleAsync(x => x.MaterialRequestId == reqId)).Id;
        }
        var approved = await manager.PostAsJsonAsync($"/api/workflows/{workflowId}/approve", new { note = "Multi-match approved" });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        using (var db = fixture.Context())
        {
            var lA = await db.Listings.FindAsync(listingAId);
            var lB = await db.Listings.FindAsync(listingBId);
            Assert.Equal(20m, lA!.ReservedQuantity);
            Assert.Equal(ListingStatus.RESERVED, lA.Status);
            Assert.Equal(30m, lB!.ReservedQuantity);
            Assert.Equal(ListingStatus.RESERVED, lB.Status);

            var reservations = await db.Reservations.Where(x => x.MaterialRequestId == reqId).ToListAsync();
            Assert.Equal(2, reservations.Count);
            Assert.Contains(reservations, r => r.ListingId == listingAId && r.Quantity == 20m);
            Assert.Contains(reservations, r => r.ListingId == listingBId && r.Quantity == 30m);

            var req = await db.BuyerRequests.FindAsync(reqId);
            Assert.Equal(BuyerRequestStatus.APPROVED, req!.Status);

            var txs = await db.Transactions.Where(x => x.Offer.MaterialMatch.MaterialRequestId == reqId).ToListAsync();
            Assert.Equal(2, txs.Count);
            Assert.Contains(txs, t => t.SellerId == fixture.Seller && t.Quantity == 20m && t.ReservedQuantity == 20m && t.Status == TransactionStatus.APPROVED);
            Assert.Contains(txs, t => t.SellerId == fixture.OtherBuyer && t.Quantity == 30m && t.ReservedQuantity == 30m && t.Status == TransactionStatus.APPROVED);
        }
    }

    private async Task<AgentWorkflow> SeedWorkflow(AgentWorkflowStatus status)
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Workflow request",
            RequiredQuantity = 2, MaximumBudget = 1000, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.MATCH_FOUND
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Workflow listing",
            Quantity = 10, ReservedQuantity = 0, Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        var match = new MaterialMatch { Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Status = MatchStatus.ROUTED, Distance = 10, DurationMinutes = 30, EstimatedTransportCost = 100 };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, MaterialMatchId = match.Id, Status = status,
            CurrentStage = "REVIEW", InputJson = "{\"source\":\"test\"}", OutputJson = "{\"quantity\":2}",
            ValidationJson = "{\"valid\":true}", StartedAtUtc = DateTime.UtcNow
        };
        db.AddRange(request, listing, match, workflow);
        await db.SaveChangesAsync();
        return workflow;
    }
}
