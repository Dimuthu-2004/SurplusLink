using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MarketplaceRoleIntegrityTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Database_rejects_self_dealing_offers_and_transactions()
    {
        var seed = await SeedLegitimatePairAsync();
        using var db = fixture.Context();

        db.Offers.Add(new Offer
        {
            Id = Guid.NewGuid(), MaterialMatchId = seed.MatchId, BuyerId = fixture.Buyer, SellerId = fixture.Buyer,
            Quantity = 1, UnitValue = 10, TotalValue = 10, Status = OfferStatus.PENDING
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), OfferId = seed.OfferId, BuyerId = fixture.Buyer, SellerId = fixture.Buyer,
            Quantity = 1, TotalValue = 10, Status = TransactionStatus.PENDING_APPROVAL
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Legitimate_buyer_seller_counterparties_can_create_offer_and_transaction_records()
    {
        var seed = await SeedLegitimatePairAsync();
        using var db = fixture.Context();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(), OfferId = seed.OfferId, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = 1, TotalValue = 10, Status = TransactionStatus.PENDING_APPROVAL
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        Assert.Equal(fixture.Buyer, (await db.Offers.FindAsync(seed.OfferId))!.BuyerId);
        Assert.Equal(fixture.Seller, (await db.Offers.FindAsync(seed.OfferId))!.SellerId);
        Assert.Equal(TransactionStatus.PENDING_APPROVAL, (await db.Transactions.FindAsync(transaction.Id))!.Status);
        Assert.NotEqual(fixture.Buyer, fixture.Seller);
    }

    private async Task<(Guid MatchId, Guid OfferId)> SeedLegitimatePairAsync()
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Integrity request",
            RequiredQuantity = 1, MaximumBudget = 100, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.MATCH_FOUND
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Integrity listing",
            Quantity = 5, Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        var match = new MaterialMatch { Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id };
        var offer = new Offer
        {
            Id = Guid.NewGuid(), MaterialMatchId = match.Id, BuyerId = fixture.Buyer, SellerId = fixture.Seller,
            Quantity = 1, UnitValue = 10, TotalValue = 10, Status = OfferStatus.PENDING
        };
        db.AddRange(request, listing, match, offer);
        await db.SaveChangesAsync();
        return (match.Id, offer.Id);
    }
}
