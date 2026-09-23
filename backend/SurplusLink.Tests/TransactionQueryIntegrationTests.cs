using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Transactions;

namespace SurplusLink.Tests;

public sealed class TransactionQueryIntegrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Marketplace_roles_read_only_participant_records_and_cannot_decide()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        foreach (var (id, roles) in new[] {
            (fixture.Seller, new[] { "SELLER" }), (fixture.Buyer, new[] { "BUYER" }),
            (fixture.Buyer, new[] { "SELLER", "BUYER" }), (fixture.Seller, new[] { "SELLER", "BUYER" }) })
        {
            using var participant = fixture.Client(app, id, roles[0], roles.Skip(1).ToArray());
            var offers = await participant.GetFromJsonAsync<JsonElement>("/api/offers?userId=" + fixture.OtherBuyer);
            Assert.Contains(offers.GetProperty("items").EnumerateArray(), row => row.GetProperty("id").GetGuid() == seeded.Offer.Id);
            foreach (var path in new[] { $"offers/{seeded.Offer.Id}", $"transactions/{seeded.Pending.Id}", $"transactions/{seeded.Pending.Id}/history" })
                Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/api/" + path)).StatusCode);
            foreach (var action in new[] { "approve", "reject", "revise" })
                Assert.Equal(HttpStatusCode.Forbidden, (await participant.PostAsync($"/api/offers/{seeded.Offer.Id}/{action}", null)).StatusCode);
            Assert.Equal(id == fixture.Buyer ? HttpStatusCode.Conflict : HttpStatusCode.Forbidden, (await participant.PostAsync($"/api/transactions/{seeded.Pending.Id}/complete", null)).StatusCode);
        }
        using var unrelated = fixture.Client(app, fixture.OtherBuyer, "SELLER", "BUYER");
        var empty = await unrelated.GetFromJsonAsync<JsonElement>("/api/offers?userId=" + fixture.Buyer);
        Assert.Equal(0, empty.GetProperty("total").GetInt32());
        foreach (var path in new[] { $"offers/{seeded.Offer.Id}", $"transactions/{seeded.Pending.Id}", $"transactions/{seeded.Pending.Id}/history" })
            Assert.Equal(HttpStatusCode.Forbidden, (await unrelated.GetAsync("/api/" + path)).StatusCode);
    }

    [PostgresFact]
    public async Task Manager_can_filter_sort_history_analytics_but_only_participants_transfer()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");

        Assert.Equal(HttpStatusCode.OK, (await buyer.GetAsync("/api/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsync($"/api/transactions/{seeded.Pending.Id}/approve", null)).StatusCode);
        using var otherBuyer = fixture.Client(app, fixture.OtherBuyer, "BUYER");
        var privatePage = await otherBuyer.GetFromJsonAsync<JsonElement>("/api/transactions?userId=" + fixture.Buyer);
        Assert.Equal(0, privatePage.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await otherBuyer.GetAsync($"/api/transactions/{seeded.Pending.Id}/history")).StatusCode);
        foreach (var path in new[] { $"/api/offers/{seeded.Offer.Id}", $"/api/transactions/{seeded.Pending.Id}" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await otherBuyer.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await buyer.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(path)).StatusCode);
        }
        var page = await manager.GetFromJsonAsync<JsonElement>(
            "/api/transactions?status=pending_approval&userId=" + fixture.Buyer + "&sortBy=value&sortDir=asc&pageSize=1");
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal(seeded.Pending.Id, page.GetProperty("items")[0].GetProperty("id").GetGuid());

        var scoped = await manager.GetFromJsonAsync<JsonElement>("/api/transactions?matchId=" + seeded.Offer.MaterialMatchId);
        Assert.Equal(seeded.Pending.Id, Assert.Single(scoped.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var byOffer = await buyer.GetFromJsonAsync<JsonElement>("/api/transactions?offerId=" + seeded.Offer.Id);
        Assert.Equal(seeded.Pending.Id, Assert.Single(byOffer.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var approved = await manager.PostAsync($"/api/transactions/{seeded.Pending.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync($"/api/transactions/{seeded.Pending.Id}/complete", null)).StatusCode);
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync($"/api/transactions/{seeded.Pending.Id}/handover", null)).StatusCode);
        var completed = await buyer.PostAsync($"/api/transactions/{seeded.Pending.Id}/confirm-receipt", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        var history = await manager.GetFromJsonAsync<TransactionHistoryPage>(
            $"/api/transactions/{seeded.Pending.Id}/history?pageSize=10");
        Assert.Equal(new[] { "TRANSACTION_COMPLETED", "TRANSACTION_HANDED_OVER", "WORKFLOW_APPROVED" },
            history!.Items.Select(x => x.Action).OrderBy(x => x));

        var summary = await manager.GetFromJsonAsync<TransactionAnalyticsSummary>("/api/transactions/analytics/summary");
        Assert.True(summary!.CompletionCount >= 1);
        Assert.True(summary.CompletedValue >= seeded.Pending.TotalValue);
        Assert.True(summary.ReservedQuantity >= seeded.Pending.Quantity);
    }

    [PostgresFact]
    public async Task Offer_decisions_are_audited_and_query_validation_is_enforced()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/offers/{seeded.Offer.Id}/revise", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsync($"/api/offers/{seeded.Offer.Id}/reject", null)).StatusCode);
        var offers = await manager.GetFromJsonAsync<JsonElement>("/api/offers?status=revision_requested&sortBy=status&sortDir=desc");
        Assert.Contains(offers.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == seeded.Offer.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync("/api/transactions?sortBy=bad")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync("/api/offers?createdFrom=2030-01-02&createdTo=2030-01-01")).StatusCode);

        using var db = fixture.Context();
        var actions = await db.AuditLogs.Where(x => x.EntityId == seeded.Offer.Id).Select(x => x.Action).ToListAsync();
        Assert.Contains("OFFER_REVISION_REQUESTED", actions);
        Assert.DoesNotContain("OFFER_REJECTED", actions);
    }

    [PostgresFact]
    public async Task Approval_reserves_handover_waits_and_buyer_receipt_completes_with_private_contacts()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        var path = $"/api/transactions/{seeded.Pending.Id}";
        foreach (var participant in new[] { buyer, seller })
        {
            var pending = await participant.GetFromJsonAsync<TransactionResponse>(path);
            Assert.Null(pending!.BuyerContact);
            Assert.Null(pending.SellerContact);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync(path + "/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync(path + "/approve", null)).StatusCode);
        await AssertState(TransactionStatus.APPROVED, BuyerRequestStatus.APPROVED, ReservationStatus.ACTIVE, AgentWorkflowStatus.APPROVED);
        foreach (var participant in new[] { buyer, seller })
        {
            var approved = await participant.GetFromJsonAsync<TransactionResponse>(path);
            Assert.Equal(fixture.Buyer + "@requirements.test", approved!.BuyerContact!.Email);
            Assert.Equal(fixture.Seller + "@requirements.test", approved.SellerContact!.Email);
        }
        Assert.Null((await manager.GetFromJsonAsync<TransactionResponse>(path))!.BuyerContact);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        await AssertState(TransactionStatus.HANDED_OVER, BuyerRequestStatus.APPROVED, ReservationStatus.ACTIVE, AgentWorkflowStatus.APPROVED);
        Assert.Equal(HttpStatusCode.Conflict, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        await AssertState(TransactionStatus.COMPLETED, BuyerRequestStatus.COMPLETED, ReservationStatus.CONFIRMED, AgentWorkflowStatus.COMPLETED);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        var history = await buyer.GetFromJsonAsync<TransactionHistoryPage>(path + "/history");
        Assert.Equal(new[] { "WORKFLOW_APPROVED", "TRANSACTION_HANDED_OVER", "TRANSACTION_COMPLETED" }, history!.Items.Select(x => x.Action));

        async Task AssertState(TransactionStatus status, BuyerRequestStatus requestStatus, ReservationStatus reservationStatus, AgentWorkflowStatus workflowStatus)
        {
            using var db = fixture.Context();
            var row = await db.Transactions.Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.MaterialRequest).SingleAsync(x => x.Id == seeded.Pending.Id);
            Assert.Equal(status, row.Status);
            Assert.Equal(row.Quantity, row.ReservedQuantity);
            Assert.Equal(status == TransactionStatus.COMPLETED, row.CompletedAtUtc.HasValue);
            Assert.Equal(requestStatus, row.Offer.MaterialMatch.MaterialRequest.Status);
            var reservation = Assert.Single(await db.Reservations.Where(x => x.MaterialRequestId == row.Offer.MaterialMatch.MaterialRequestId).ToListAsync());
            Assert.Equal(reservationStatus, reservation.Status);
            Assert.Equal(row.Quantity, reservation.Quantity);
            Assert.Equal(row.Quantity, (await db.Listings.SingleAsync(x => x.Id == reservation.ListingId)).ReservedQuantity);
            var workflow = await db.AgentWorkflows.SingleAsync(x => x.MaterialMatchId == seeded.Offer.MaterialMatchId);
            Assert.Equal(workflowStatus, workflow.Status);
            Assert.Equal(status == TransactionStatus.COMPLETED, workflow.CompletedAtUtc.HasValue);
        }
    }

    [PostgresFact]
    public async Task Non_seller_cannot_handover_and_non_buyer_cannot_complete_even_with_both_roles()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER", "SELLER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER", "BUYER");
        using var unrelated = fixture.Client(app, fixture.OtherBuyer, "BUYER", "SELLER");
        var path = $"/api/transactions/{seeded.Pending.Id}";
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync(path + "/approve", null)).StatusCode);
        foreach (var client in new[] { buyer, unrelated, manager })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(path + "/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        foreach (var client in new[] { seller, unrelated, manager })
            foreach (var action in new[] { "/complete", "/confirm-receipt" })
                Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(path + action, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await unrelated.GetAsync(path)).StatusCode);
        Assert.Equal(TransactionStatus.HANDED_OVER, (await buyer.GetFromJsonAsync<TransactionResponse>(path))!.Status);
    }

    private async Task<(Offer Offer, Transaction Pending)> Seed()
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Transaction request",
            RequiredQuantity = 3, MaximumBudget = 1000, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.MATCH_FOUND
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Transaction listing",
            Quantity = 20, Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        var match = new MaterialMatch { Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Status = MatchStatus.ROUTED, Distance = 10, DurationMinutes = 30, EstimatedTransportCost = 100 };
        var offer = new Offer
        {
            Id = Guid.NewGuid(), MaterialMatchId = match.Id, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = 3, UnitValue = 10, TotalValue = 30, Status = OfferStatus.PENDING
        };
        var pending = new Transaction
        {
            Id = Guid.NewGuid(), OfferId = offer.Id, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = 3, TotalValue = 30, Status = TransactionStatus.PENDING_APPROVAL
        };
        var workflow = new AgentWorkflow { Id = Guid.NewGuid(), MaterialRequestId = request.Id,
            MaterialMatchId = match.Id, Status = AgentWorkflowStatus.PENDING_APPROVAL, CurrentStage = "PENDING_APPROVAL",
            ValidationJson = "{\"valid\":true}", StartedAtUtc = DateTime.UtcNow };
        db.AddRange(request, listing, match, offer, pending, workflow);
        await db.SaveChangesAsync();
        return (offer, pending);
    }
}
