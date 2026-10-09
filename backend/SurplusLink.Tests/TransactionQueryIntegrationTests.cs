using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Transactions;

namespace SurplusLink.Tests;

public sealed class TransactionQueryIntegrationTests : IAsyncLifetime
{
    // Each test owns its database: pending offers and profile edits must not
    // leak into another test's count, pagination, or participant assertions.
    private readonly RequirementsDatabase fixture = new();

    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();

    [PostgresFact]
    public async Task Offer_and_group_reads_resolve_names_without_disclosing_private_profile_fields()
    {
        var seeded = await Seed();
        Guid workflowId;
        using (var db = fixture.Context())
        {
            var buyer = await db.Users.SingleAsync(x => x.Id == fixture.Buyer);
            var seller = await db.Users.SingleAsync(x => x.Id == fixture.Seller);
            buyer.FullName = "Nimal Perera";
            seller.FullName = "Kamal Silva";
            seller.BusinessName = "ABC Materials";
            await db.SaveChangesAsync();
            workflowId = await db.AgentWorkflows.Where(x => x.MaterialMatchId == seeded.Offer.MaterialMatchId).Select(x => x.Id).SingleAsync();
        }
        using var app = fixture.App();
        using var buyerClient = fixture.Client(app, fixture.Buyer, "BUYER");
        using var sellerClient = fixture.Client(app, fixture.Seller, "SELLER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        foreach (var client in new[] { buyerClient, sellerClient, manager })
        {
            var offer = await client.GetFromJsonAsync<OfferResponse>($"/api/offers/{seeded.Offer.Id}");
            Assert.Equal("Nimal Perera", offer!.BuyerName);
            Assert.Equal("Kamal Silva", offer.SellerName);
            Assert.Equal("ABC Materials", offer.SellerBusinessName);
            Assert.Equal("Transaction listing", offer.MaterialName);
            Assert.Equal("kg", offer.Unit);
            Assert.Equal(seeded.Offer.BuyerId, offer.BuyerId);
            var list = await client.GetFromJsonAsync<JsonElement>("/api/offers");
            var row = list.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == seeded.Offer.Id);
            Assert.Equal("Nimal Perera", row.GetProperty("buyerName").GetString());
            Assert.False(row.TryGetProperty("nic", out _));
            Assert.False(row.TryGetProperty("email", out _));
            Assert.False(row.TryGetProperty("passwordHash", out _));
        }
        var workflow = await manager.GetFromJsonAsync<JsonElement>($"/api/workflows/{workflowId}");
        var group = workflow.GetProperty("approvalGroup");
        Assert.Equal("Nimal Perera", group.GetProperty("buyerName").GetString());
        Assert.Equal("ABC Materials", group.GetProperty("allocations")[0].GetProperty("sellerBusinessName").GetString());
    }

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
        using var completed = await buyer.PostAsync($"/api/transactions/{seeded.Pending.Id}/confirm-receipt", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(0, (await completed.Content.ReadFromJsonAsync<TransactionResponse>())!.ReservedQuantity);

        var history = await manager.GetFromJsonAsync<TransactionHistoryPage>(
            $"/api/transactions/{seeded.Pending.Id}/history?pageSize=10");
        Assert.Equal(new[] { "BUYER_RECEIPT_CONFIRMED_TRANSACTION_COMPLETED", "SELLER_HANDOVER_CONFIRMED", "TRANSACTION_APPROVED_STOCK_RESERVED", "WORKFLOW_APPROVED" },
            history!.Items.Select(x => x.Action).OrderBy(x => x));

        var summary = await manager.GetFromJsonAsync<TransactionAnalyticsSummary>("/api/transactions/analytics/summary");
        Assert.True(summary!.CompletionCount >= 1);
        Assert.True(summary.CompletedValue >= seeded.Pending.TotalValue);
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
        var managerApproved = await manager.GetFromJsonAsync<TransactionResponse>(path);
        Assert.Equal(fixture.Buyer + "@requirements.test", managerApproved!.BuyerContact!.Email);
        Assert.Equal(fixture.Seller + "@requirements.test", managerApproved.SellerContact!.Email);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        await AssertState(TransactionStatus.HANDED_OVER, BuyerRequestStatus.APPROVED, ReservationStatus.ACTIVE, AgentWorkflowStatus.APPROVED);
        Assert.Equal(HttpStatusCode.Conflict, (await seller.PostAsync(path + "/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        await AssertState(TransactionStatus.COMPLETED, BuyerRequestStatus.COMPLETED, ReservationStatus.CONFIRMED, AgentWorkflowStatus.COMPLETED);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/confirm-receipt", null)).StatusCode);
        var history = await buyer.GetFromJsonAsync<TransactionHistoryPage>(path + "/history");
        var actions = history!.Items.Select(x => x.Action).ToArray();
        Assert.Equal(new[] { "TRANSACTION_APPROVED_STOCK_RESERVED", "WORKFLOW_APPROVED" }, actions.Take(2).OrderBy(x => x));
        Assert.Equal(new[] { "SELLER_HANDOVER_CONFIRMED", "BUYER_RECEIPT_CONFIRMED_TRANSACTION_COMPLETED" }, actions.Skip(2));

        async Task AssertState(TransactionStatus status, BuyerRequestStatus requestStatus, ReservationStatus reservationStatus, AgentWorkflowStatus workflowStatus)
        {
            using var db = fixture.Context();
            var row = await db.Transactions.Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.MaterialRequest).SingleAsync(x => x.Id == seeded.Pending.Id);
            Assert.Equal(status, row.Status);
            Assert.Equal(status == TransactionStatus.COMPLETED ? 0 : row.Quantity, row.ReservedQuantity);
            Assert.Equal(status == TransactionStatus.COMPLETED, row.CompletedAtUtc.HasValue);
            Assert.Equal(requestStatus, row.Offer.MaterialMatch.MaterialRequest.Status);
            var reservation = Assert.Single(await db.Reservations.Where(x => x.MaterialRequestId == row.Offer.MaterialMatch.MaterialRequestId).ToListAsync());
            Assert.Equal(reservationStatus, reservation.Status);
            Assert.Equal(row.Quantity, reservation.Quantity);
            Assert.Equal(status == TransactionStatus.COMPLETED ? 0 : row.Quantity,
                (await db.Listings.SingleAsync(x => x.Id == reservation.ListingId)).ReservedQuantity);
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

    [PostgresFact]
    public async Task Manager_not_completed_releases_only_the_reserved_physical_allocation()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        var path = $"/api/transactions/{seeded.Pending.Id}";
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync(path + "/approve", null)).StatusCode);
        var response = await manager.PostAsJsonAsync(path + "/resolve-not-completed", new { note = "Confirmed neither party completed handover." });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var db = fixture.Context();
        var row = await db.Transactions.SingleAsync(x => x.Id == seeded.Pending.Id);
        var reservation = await db.Reservations.SingleAsync(x => x.TransactionId == row.Id);
        var listing = await db.Listings.SingleAsync(x => x.Id == reservation.ListingId);
        Assert.Equal(TransactionStatus.NOT_COMPLETED, row.Status);
        Assert.Equal(0, row.ReservedQuantity);
        Assert.Equal(ReservationStatus.RELEASED, reservation.Status);
        Assert.Equal(20, listing.Quantity);
        Assert.Equal(0, listing.ReservedQuantity);
        Assert.Equal("MANAGER_MARKED_NOT_COMPLETED", row.ResolutionReasonCode);
    }

    [PostgresFact]
    public async Task Manager_completed_finalizes_the_reservation_without_claiming_buyer_receipt()
    {
        var seeded = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/transactions/{seeded.Pending.Id}/approve", null)).StatusCode);
        var response = await manager.PostAsJsonAsync($"/api/transactions/{seeded.Pending.Id}/resolve-completed", new { note = "Confirmed after speaking with both parties." });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var db = fixture.Context();
        var row = await db.Transactions.SingleAsync(x => x.Id == seeded.Pending.Id);
        var reservation = await db.Reservations.SingleAsync(x => x.TransactionId == row.Id);
        var listing = await db.Listings.SingleAsync(x => x.Id == reservation.ListingId);
        Assert.Equal(TransactionStatus.COMPLETED, row.Status);
        Assert.Null(row.BuyerReceivedConfirmedAtUtc);
        Assert.Equal(fixture.Manager, row.ResolvedByManagerId);
        Assert.Equal(ReservationStatus.CONFIRMED, reservation.Status);
        Assert.Equal(17, listing.Quantity);
        Assert.Equal(0, listing.ReservedQuantity);
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityId == row.Id).Select(x => x.Action).ToListAsync(), action => action == "COMPLETED_BY_MANAGER_AFTER_FOLLOW_UP");
    }

    [PostgresFact]
    public async Task Deadline_without_handover_releases_but_handover_without_receipt_requires_manager_review()
    {
        var noHandover = await Seed();
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/transactions/{noHandover.Pending.Id}/approve", null)).StatusCode);
        await Expire(noHandover.Pending.Id);
        await AssertNoHandoverReleased(noHandover.Pending.Id);

        var handedOver = await Seed();
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/transactions/{handedOver.Pending.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync($"/api/transactions/{handedOver.Pending.Id}/handover", null)).StatusCode);
        await Expire(handedOver.Pending.Id);
        using var verify = fixture.Context();
        var row = await verify.Transactions.SingleAsync(x => x.Id == handedOver.Pending.Id);
        var reservation = await verify.Reservations.SingleAsync(x => x.TransactionId == row.Id);
        var listing = await verify.Listings.SingleAsync(x => x.Id == reservation.ListingId);
        Assert.Equal(TransactionStatus.MANAGER_REVIEW_REQUIRED, row.Status);
        Assert.NotNull(row.SellerHandoverConfirmedAtUtc);
        Assert.Equal(ReservationStatus.ACTIVE, reservation.Status);
        Assert.Equal(3, listing.ReservedQuantity);

        async Task Expire(Guid id)
        {
            using (var db = fixture.Context())
            {
                var row = await db.Transactions.SingleAsync(x => x.Id == id);
                row.ConfirmationDeadlineUtc = DateTime.UtcNow.AddSeconds(-1);
                await db.SaveChangesAsync();
            }
            using var processorDb = fixture.Context();
            await new TransactionService(processorDb).ProcessDeadlineAsync(id, DateTime.UtcNow, CancellationToken.None);
        }
        async Task AssertNoHandoverReleased(Guid id)
        {
            using var db = fixture.Context();
            var row = await db.Transactions.SingleAsync(x => x.Id == id);
            var reservation = await db.Reservations.SingleAsync(x => x.TransactionId == row.Id);
            var listing = await db.Listings.SingleAsync(x => x.Id == reservation.ListingId);
            Assert.Equal(TransactionStatus.NOT_COMPLETED, row.Status);
            Assert.Equal(ReservationStatus.RELEASED, reservation.Status);
            Assert.Equal(0, listing.ReservedQuantity);
            Assert.Equal("CONFIRMATION_TIMEOUT_NO_HANDOVER", row.ResolutionReasonCode);
        }
    }

    [PostgresFact]
    public async Task Multi_seller_allocations_complete_parent_only_after_every_child_transaction_completes()
    {
        var seeded = await SeedMultiSellerLifecycleAsync();
        using (var db = fixture.Context())
        {
            var offers = await db.Offers.Where(x => x.MaterialMatch.MaterialRequestId == seeded.RequestId).ToListAsync();
            var transactions = await db.Transactions.Where(x => x.Offer.MaterialMatch.MaterialRequestId == seeded.RequestId).ToListAsync();

            Assert.Equal(2, offers.Count);
            Assert.Equal(2, transactions.Count);
            Assert.Equal(2, offers.Select(x => x.Id).Distinct().Count());
            Assert.Equal(2, transactions.Select(x => x.Id).Distinct().Count());
            Assert.Equal(2, offers.Select(x => x.SellerId).Distinct().Count());
        }

        using (var db = fixture.Context())
            await new TransactionService(db).CompleteAsync(seeded.TransactionAId, fixture.Buyer, CancellationToken.None);

        using (var db = fixture.Context())
        {
            Assert.Equal(BuyerRequestStatus.APPROVED, (await db.BuyerRequests.FindAsync(seeded.RequestId))!.Status);
            Assert.Equal(TransactionStatus.COMPLETED, (await db.Transactions.FindAsync(seeded.TransactionAId))!.Status);
            Assert.Equal(TransactionStatus.HANDED_OVER, (await db.Transactions.FindAsync(seeded.TransactionBId))!.Status);
            var bReservation = await db.Reservations.SingleAsync(x => x.TransactionId == seeded.TransactionBId);
            Assert.Equal(ReservationStatus.ACTIVE, bReservation.Status);
            Assert.Equal(3, (await db.Listings.SingleAsync(x => x.Id == bReservation.ListingId)).ReservedQuantity);
        }

        using (var db = fixture.Context())
            await new TransactionService(db).CompleteAsync(seeded.TransactionBId, fixture.Buyer, CancellationToken.None);

        using (var db = fixture.Context())
        {
            Assert.Equal(BuyerRequestStatus.COMPLETED, (await db.BuyerRequests.FindAsync(seeded.RequestId))!.Status);
            Assert.All(await db.Transactions.Where(x => x.Offer.MaterialMatch.MaterialRequestId == seeded.RequestId).ToListAsync(),
                transaction => Assert.Equal(TransactionStatus.COMPLETED, transaction.Status));
        }
    }

    [PostgresFact]
    public async Task Piece_exact_sell_out_completes_and_consumes_all_stock()
    {
        var seeded = await SeedCompletionAsync(QuantityMode.PIECE, 25, 25);
        await AssertCompletionAsync(seeded, ListingStatus.SOLD, 0, 0);
    }

    [PostgresFact]
    public async Task Package_exact_sell_out_completes_and_consumes_all_stock()
    {
        var seeded = await SeedCompletionAsync(QuantityMode.PACKAGE, 25, 25);
        await AssertCompletionAsync(seeded, ListingStatus.SOLD, 0, 0);
    }

    [PostgresFact]
    public async Task Continuous_exact_sell_out_completes_and_consumes_all_stock()
    {
        var seeded = await SeedCompletionAsync(QuantityMode.CONTINUOUS, 25, 25);
        await AssertCompletionAsync(seeded, ListingStatus.SOLD, null, 0);
    }

    [PostgresFact]
    public async Task Partial_completion_leaves_remaining_stock_active()
    {
        var seeded = await SeedCompletionAsync(QuantityMode.PIECE, 25, 20);
        await AssertCompletionAsync(seeded, ListingStatus.ACTIVE, 5, 5);
    }

    [PostgresFact]
    public async Task Listing_stock_constraints_reject_negative_quantity_and_package_counts()
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Constrained stock",
            Description = "Stock constraint regression", Quantity = 1, ReservedQuantity = 0,
            QuantityMode = QuantityMode.PIECE, PackageType = PackageType.PIECE, PackageSize = 1,
            PackageCount = 1, ReservedPackageCount = 0, Unit = "piece", UnitPrice = 1,
            AvailableUntil = DateTime.UtcNow.AddDays(5), Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        listing.Quantity = -1;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.Entry(listing).State = EntityState.Detached;

        var packaged = await db.Listings.SingleAsync(x => x.Id == listing.Id);
        packaged.ReservedPackageCount = -1;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Multi_seller_parent_is_not_completed_when_one_child_is_completed_and_another_is_not_completed()
    {
        var seeded = await SeedMultiSellerLifecycleAsync();

        using (var db = fixture.Context())
            await new TransactionService(db).CompleteAsync(seeded.TransactionAId, fixture.Buyer, CancellationToken.None);

        using (var db = fixture.Context())
            await new TransactionService(db).ResolveNotCompletedAsync(seeded.TransactionBId, fixture.Manager,
                "The second seller allocation was not handed over.", CancellationToken.None);

        using var verify = fixture.Context();
        Assert.Equal(BuyerRequestStatus.APPROVED, (await verify.BuyerRequests.FindAsync(seeded.RequestId))!.Status);
        Assert.Equal(TransactionStatus.COMPLETED, (await verify.Transactions.FindAsync(seeded.TransactionAId))!.Status);
        Assert.Equal(TransactionStatus.NOT_COMPLETED, (await verify.Transactions.FindAsync(seeded.TransactionBId))!.Status);
    }

    private async Task<(Offer Offer, Transaction Pending)> Seed()
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Transaction listing",
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

    private async Task<(Guid TransactionId, Guid ReservationId, Guid ListingId)> SeedCompletionAsync(
        QuantityMode mode, decimal availableQuantity, decimal selectedQuantity)
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var packaged = mode is QuantityMode.PIECE or QuantityMode.PACKAGE;
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Completion stock regression",
            RequiredQuantity = selectedQuantity, MaximumBudget = 1_000, Unit = "piece", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.APPROVED
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Completion stock listing",
            Description = "Exact sold-out completion coverage", Quantity = availableQuantity, ReservedQuantity = selectedQuantity,
            QuantityMode = mode, PackageType = packaged ? PackageType.PIECE : null, PackageSize = packaged ? 1 : null,
            PackageCount = packaged ? (int)availableQuantity : null, ReservedPackageCount = packaged ? (int)selectedQuantity : 0,
            Unit = "piece", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        var match = new MaterialMatch
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Status = MatchStatus.ROUTED,
            Distance = 1, DurationMinutes = 1, EstimatedTransportCost = 1
        };
        var offer = new Offer
        {
            Id = Guid.NewGuid(), MaterialMatchId = match.Id, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = selectedQuantity, UnitValue = 10, TotalValue = selectedQuantity * 10, Status = OfferStatus.ACCEPTED,
            PackageCount = packaged ? (int)selectedQuantity : null
        };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(), OfferId = offer.Id, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = selectedQuantity, PackageCount = packaged ? (int)selectedQuantity : null,
            TotalValue = offer.TotalValue, ReservedQuantity = selectedQuantity, Status = TransactionStatus.HANDED_OVER,
            SellerHandoverConfirmedAtUtc = DateTime.UtcNow
        };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, TransactionId = transaction.Id,
            Quantity = selectedQuantity, PackageCount = packaged ? (int)selectedQuantity : null, Status = ReservationStatus.ACTIVE
        };
        db.AddRange(request, listing, match, offer, transaction, reservation);
        await db.SaveChangesAsync();
        return (transaction.Id, reservation.Id, listing.Id);
    }

    private async Task AssertCompletionAsync((Guid TransactionId, Guid ReservationId, Guid ListingId) seeded,
        ListingStatus expectedStatus, int? expectedPackageCount, decimal expectedQuantity)
    {
        using (var db = fixture.Context())
            await new TransactionService(db).CompleteAsync(seeded.TransactionId, fixture.Buyer, CancellationToken.None);
        using var verify = fixture.Context();
        var transaction = await verify.Transactions.SingleAsync(x => x.Id == seeded.TransactionId);
        var reservation = await verify.Reservations.SingleAsync(x => x.Id == seeded.ReservationId);
        var listing = await verify.Listings.SingleAsync(x => x.Id == seeded.ListingId);
        Assert.Equal(TransactionStatus.COMPLETED, transaction.Status);
        Assert.Equal(0, transaction.ReservedQuantity);
        Assert.Equal(ReservationStatus.CONFIRMED, reservation.Status);
        Assert.Equal(expectedQuantity, listing.Quantity);
        Assert.Equal(0, listing.ReservedQuantity);
        Assert.Equal(expectedPackageCount, listing.PackageCount);
        Assert.Equal(0, listing.ReservedPackageCount);
        Assert.Equal(expectedStatus, listing.Status);
    }

    private async Task<(Guid RequestId, Guid TransactionAId, Guid TransactionBId)> SeedMultiSellerLifecycleAsync()
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var sellerB = Guid.NewGuid();
        db.Users.Add(new User { Id = sellerB, Email = sellerB + "@requirements.test", PasswordHash = "unused-test-hash" });
        db.Set<UserRoleAssignment>().Add(new UserRoleAssignment { UserId = sellerB, Role = UserRole.SELLER });
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Multi-seller transaction lifecycle",
            RequiredQuantity = 5, MaximumBudget = 1000, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.APPROVED
        };
        var allocations = new[]
        {
            (SellerId: fixture.Seller, Quantity: 2m, Title: "Seller A allocation"),
            (SellerId: sellerB, Quantity: 3m, Title: "Seller B allocation")
        };
        var transactions = new List<Transaction>();
        foreach (var allocation in allocations)
        {
            var listing = new Listing
            {
                Id = Guid.NewGuid(), SellerId = allocation.SellerId, CategoryId = category.Id, Title = allocation.Title,
                Quantity = 10, ReservedQuantity = allocation.Quantity, Unit = "kg", UnitPrice = 10,
                AvailableUntil = DateTime.UtcNow.AddDays(5), Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
            };
            var match = new MaterialMatch
            {
                Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Status = MatchStatus.ROUTED,
                Distance = 10, DurationMinutes = 30, EstimatedTransportCost = 100
            };
            var offer = new Offer
            {
                Id = Guid.NewGuid(), MaterialMatchId = match.Id, BuyerId = fixture.Buyer, SellerId = allocation.SellerId,
                Quantity = allocation.Quantity, UnitValue = 10, TotalValue = allocation.Quantity * 10, Status = OfferStatus.ACCEPTED
            };
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(), OfferId = offer.Id, BuyerId = fixture.Buyer, SellerId = allocation.SellerId,
                Quantity = allocation.Quantity, TotalValue = offer.TotalValue, ReservedQuantity = allocation.Quantity,
                Status = TransactionStatus.HANDED_OVER, SellerHandoverConfirmedAtUtc = DateTime.UtcNow
            };
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, TransactionId = transaction.Id,
                Quantity = allocation.Quantity, Status = ReservationStatus.ACTIVE
            };
            db.AddRange(listing, match, offer, transaction, reservation);
            transactions.Add(transaction);
        }
        db.BuyerRequests.Add(request);
        await db.SaveChangesAsync();
        return (request.Id, transactions[0].Id, transactions[1].Id);
    }
}
