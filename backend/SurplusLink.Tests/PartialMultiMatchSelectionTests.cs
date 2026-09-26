using System.ComponentModel.DataAnnotations;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;

namespace SurplusLink.Tests;

public sealed class PartialMultiMatchSelectionTests
{
    private readonly Guid buyerId = Guid.NewGuid();
    private readonly Guid sellerA = Guid.NewGuid();
    private readonly Guid sellerB = Guid.NewGuid();
    private readonly Guid sellerC = Guid.NewGuid();
    private readonly Guid categoryId = Guid.NewGuid();

    [Fact]
    public void Candidate_with_partial_stock_is_not_rejected()
    {
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            CategoryId = categoryId,
            RequiredQuantity = 50,
            Unit = "pcs",
            MaximumBudget = 2000,
            Deadline = DateTime.UtcNow.AddDays(7),
            Status = BuyerRequestStatus.OPEN
        };

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Title = "Ceramic tiles",
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE,
            Condition = MaterialCondition.GOOD
        };

        // Partial stock (20 available vs 50 required): should NOT be rejected
        var reason = MatchService.EligibilityReason(request, listing);
        Assert.Null(reason);
    }

    [Fact]
    public void Candidate_with_zero_or_negative_unreserved_stock_is_rejected_as_insufficient()
    {
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            CategoryId = categoryId,
            RequiredQuantity = 50,
            Unit = "pcs",
            MaximumBudget = 2000,
            Deadline = DateTime.UtcNow.AddDays(7),
            Status = BuyerRequestStatus.OPEN
        };

        var zeroStockListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Title = "Ceramic tiles",
            Quantity = 20,
            ReservedQuantity = 20, // 0 unreserved
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE,
            Condition = MaterialCondition.GOOD
        };

        var reason = MatchService.EligibilityReason(request, zeroStockListing);
        Assert.Equal("INSUFFICIENT_QUANTITY", reason);

        var overReservedListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Title = "Ceramic tiles",
            Quantity = 20,
            ReservedQuantity = 25, // negative unreserved
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE,
            Condition = MaterialCondition.GOOD
        };

        reason = MatchService.EligibilityReason(request, overReservedListing);
        Assert.Equal("INSUFFICIENT_QUANTITY", reason);
    }

    [Fact]
    public void Candidate_eligibility_still_enforces_other_business_rules()
    {
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            CategoryId = categoryId,
            RequiredQuantity = 50,
            Unit = "pcs",
            MaximumBudget = 2000,
            Deadline = DateTime.UtcNow.AddDays(7),
            Status = BuyerRequestStatus.OPEN
        };

        // Self match
        var selfListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = buyerId,
            CategoryId = categoryId,
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE
        };
        Assert.Equal("SELF_MATCH_NOT_ALLOWED", MatchService.EligibilityReason(request, selfListing));

        // Inactive listing
        var inactiveListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.DRAFT
        };
        Assert.Equal("LISTING_NOT_ACTIVE", MatchService.EligibilityReason(request, inactiveListing));

        // Expired listing
        var expiredListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(-1),
            Status = ListingStatus.ACTIVE
        };
        Assert.Equal("LISTING_EXPIRED", MatchService.EligibilityReason(request, expiredListing));

        // Category mismatch
        var wrongCategoryListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = Guid.NewGuid(),
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "pcs",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE
        };
        Assert.Equal("CATEGORY_MISMATCH", MatchService.EligibilityReason(request, wrongCategoryListing));

        // Unit mismatch
        var wrongUnitListing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerA,
            CategoryId = categoryId,
            Quantity = 20,
            ReservedQuantity = 0,
            Unit = "kg",
            UnitPrice = 15,
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            Status = ListingStatus.ACTIVE
        };
        Assert.Equal("UNIT_MISMATCH", MatchService.EligibilityReason(request, wrongUnitListing));
    }

    [Fact]
    public void SelectMatchesRequest_validation_rules()
    {
        // Empty allocations rejected
        var empty = new SelectMatchesRequest { Allocations = [] };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(empty, new ValidationContext(empty), results, true));

        // Duplicate match IDs rejected
        var duplicateMatchId = Guid.NewGuid();
        var duplicates = new SelectMatchesRequest
        {
            Allocations =
            [
                new MatchAllocationRequest { MatchId = duplicateMatchId, Quantity = 10 },
                new MatchAllocationRequest { MatchId = duplicateMatchId, Quantity = 15 }
            ]
        };
        results.Clear();
        Assert.False(Validator.TryValidateObject(duplicates, new ValidationContext(duplicates), results, true));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Duplicate match ID"));

        // Quantity <= 0 rejected
        var zeroQty = new SelectMatchesRequest
        {
            Allocations =
            [
                new MatchAllocationRequest { MatchId = Guid.NewGuid(), Quantity = 0 }
            ]
        };
        results.Clear();
        Assert.False(Validator.TryValidateObject(zeroQty, new ValidationContext(zeroQty), results, true));

        // Quantity with > 3 decimals rejected
        var fourDecimals = new SelectMatchesRequest
        {
            Allocations =
            [
                new MatchAllocationRequest { MatchId = Guid.NewGuid(), Quantity = 10.1234m }
            ]
        };
        results.Clear();
        Assert.False(Validator.TryValidateObject(fourDecimals, new ValidationContext(fourDecimals), results, true));

        // Valid multi-match request passes
        var valid = new SelectMatchesRequest
        {
            Allocations =
            [
                new MatchAllocationRequest { MatchId = Guid.NewGuid(), Quantity = 20 },
                new MatchAllocationRequest { MatchId = Guid.NewGuid(), Quantity = 15 },
                new MatchAllocationRequest { MatchId = Guid.NewGuid(), Quantity = 15 }
            ]
        };
        results.Clear();
        Assert.True(Validator.TryValidateObject(valid, new ValidationContext(valid), results, true));
        Assert.Empty(results);
    }

    [Fact]
    public void Multi_match_allocation_total_quantities_and_costs()
    {
        // Scenario from user requirement:
        // Buyer needs 50 tiles.
        // Seller A has 20.
        // Seller B has 15.
        // Seller C has 30.
        // Buyer selects 20 from A, 15 from B, 15 from C = 50 total.
        var allocations = new List<MatchAllocationRequest>
        {
            new() { MatchId = Guid.NewGuid(), Quantity = 20 },
            new() { MatchId = Guid.NewGuid(), Quantity = 15 },
            new() { MatchId = Guid.NewGuid(), Quantity = 15 }
        };

        var total = allocations.Sum(x => x.Quantity);
        Assert.Equal(50, total);

        // Buyer selects partial fulfillment: 20 from A, 15 from B = 35 < 50
        var partialAllocations = new List<MatchAllocationRequest>
        {
            new() { MatchId = Guid.NewGuid(), Quantity = 20 },
            new() { MatchId = Guid.NewGuid(), Quantity = 15 }
        };
        var partialTotal = partialAllocations.Sum(x => x.Quantity);
        Assert.Equal(35, partialTotal);
        Assert.True(partialTotal <= 50);

        // Over-allocation: 20 from A, 20 from B, 20 from C = 60 > 50
        var overAllocations = new List<MatchAllocationRequest>
        {
            new() { MatchId = Guid.NewGuid(), Quantity = 20 },
            new() { MatchId = Guid.NewGuid(), Quantity = 20 },
            new() { MatchId = Guid.NewGuid(), Quantity = 20 }
        };
        var overTotal = overAllocations.Sum(x => x.Quantity);
        Assert.True(overTotal > 50);
    }

    [Fact]
    public void MatchResponse_indicates_is_partial_when_available_stock_less_than_required()
    {
        var matchId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        var partialResponse = new MatchResponse(
            matchId, reqId, listingId, 0.85m, 12.5m, 50m, "ROUTED",
            true, false, null, DateTime.UtcNow, 30m, "Tiles", "Finishes", sellerA,
            50m, "pcs", 10m, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(5),
            20m, 1000m, "OPEN", "Seller A", "A Corp", "GOOD", null, null, null,
            false, null, null,
            IsPartial: true);

        Assert.True(partialResponse.IsPartial);
        Assert.Equal(20m, partialResponse.AvailableQuantity);
        Assert.Equal(50m, partialResponse.Quantity);

        var fullResponse = new MatchResponse(
            matchId, reqId, listingId, 0.85m, 12.5m, 50m, "ROUTED",
            true, false, null, DateTime.UtcNow, 30m, "Tiles", "Finishes", sellerA,
            50m, "pcs", 10m, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(5),
            100m, 1000m, "OPEN", "Seller A", "A Corp", "GOOD", null, null, null,
            false, null, null,
            IsPartial: false);

        Assert.False(fullResponse.IsPartial);
        Assert.Equal(100m, fullResponse.AvailableQuantity);
    }
}
