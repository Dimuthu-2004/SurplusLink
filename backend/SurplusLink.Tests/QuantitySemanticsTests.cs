using SurplusLink.Api.Materials;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class QuantitySemanticsTests
{
    private static readonly Guid Category = Guid.NewGuid();
    private static readonly Guid Paint = new("00000000-0000-0000-0000-000000000201");

    [Theory]
    [InlineData("L", "ml", 1, 1000)]
    [InlineData("kg", "g", 1, 1000)]
    [InlineData("m", "cm", 1, 100)]
    [InlineData("m2", "cm2", 1, 10000)]
    public void Safe_measurement_conversions_are_centralized(string from, string to, decimal amount, decimal expected)
    {
        Assert.True(QuantitySemantics.TryConvert(amount, from, to, out var converted));
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Unrelated_dimensions_are_rejected()
    {
        Assert.False(QuantitySemantics.TryConvert(2, "kg", "l", out _));
    }

    [Fact]
    public void Canned_paint_matches_litre_requirement_and_allocates_whole_cans()
    {
        var request = Request(2, "L", Paint);
        var listing = Packaged("L", 1, 5, "CAN", Paint);
        var normalized = QuantitySemantics.FromListing(listing);
        Assert.True(QuantitySemantics.IsCompatible(request, listing));
        Assert.Equal(5, normalized.AvailableBaseQuantity);
        Assert.Equal(2, QuantitySemantics.RequiredPackageCount(2, normalized));
        Assert.Null(MatchService.EligibilityReason(request, listing));
    }

    [Fact]
    public void Large_paint_can_has_valid_overage()
    {
        var quantity = QuantitySemantics.FromListing(Packaged("L", 4, 5, "CAN", Paint));
        Assert.Equal(1, QuantitySemantics.RequiredPackageCount(2, quantity));
        Assert.Equal(4, quantity.PackageSize);
    }

    [Theory]
    [InlineData("kg", 50, 10, 120, 3)]
    [InlineData("m2", 1.44, 10, 10, 7)]
    public void Packages_round_up_without_fractional_inventory(string unit, decimal size, int count, decimal required, decimal packages)
    {
        var quantity = QuantitySemantics.FromListing(Packaged(unit, size, count, "BOX", Guid.NewGuid()));
        Assert.Equal(packages, QuantitySemantics.RequiredPackageCount(required, quantity));
    }

    [Fact]
    public void Pieces_are_whole_and_continuous_stock_remains_decimal()
    {
        var bricks = QuantitySemantics.FromListing(Packaged("piece", 1, 500, "PIECE", Guid.NewGuid(), QuantityMode.PIECE));
        var sand = QuantitySemantics.FromListing(new Listing { QuantityMode = QuantityMode.CONTINUOUS, BaseUnit = "m3", Unit = "m3", Quantity = 3.5m });
        Assert.Equal(200, QuantitySemantics.RequiredPackageCount(200, bricks));
        Assert.Equal(3.5m, sand.AvailableBaseQuantity);
        Assert.Equal(.001m, sand.MinimumSellableIncrement);
    }

    [Fact]
    public void Steel_rods_and_pvc_pipes_remain_whole_physical_pieces()
    {
        var rods = QuantitySemantics.FromListing(Packaged("piece", 1, 12, "ROD", Guid.NewGuid(), QuantityMode.PIECE));
        var pipes = QuantitySemantics.FromListing(Packaged("piece", 1, 5, "PIPE", Guid.NewGuid(), QuantityMode.PIECE));
        Assert.Equal(2, QuantitySemantics.RequiredPackageCount(2, rods));
        Assert.Equal(5, QuantitySemantics.RequiredPackageCount(5, pipes));
        Assert.Equal(0, rods.DecimalPrecision);
    }

    [Fact]
    public void Partial_package_stock_remains_a_compatible_candidate()
    {
        var request = Request(10, "L", Paint);
        var listing = Packaged("L", 4, 2, "CAN", Paint);
        Assert.True(QuantitySemantics.IsCompatible(request, listing));
        Assert.Equal(8, QuantitySemantics.FromListing(listing).AvailableBaseQuantity);
    }

    [Fact]
    public void Packaged_partial_contributions_use_whole_cans_and_their_own_material_cost()
    {
        var request = Request(50, "L", Paint);
        request.MaximumBudget = 10000;
        var sellerA = Packaged("L", 4, 20, "CAN", Paint);
        sellerA.UnitPrice = 1000;
        var sellerB = Packaged("L", 4, 15, "CAN", Paint);
        sellerB.UnitPrice = 1100;

        Assert.Equal(40, QuantitySemantics.EffectiveAffordableQuantity(request, sellerA));
        Assert.Equal(10000, QuantitySemantics.MaterialCost(request, sellerA));
        Assert.Equal(36, QuantitySemantics.EffectiveAffordableQuantity(request, sellerB));
        Assert.Equal(9900, QuantitySemantics.MaterialCost(request, sellerB));
    }

    [Theory]
    [InlineData(QuantityMode.CONTINUOUS, "L", 50, 20, 1, 100, 20, false)]
    [InlineData(QuantityMode.PIECE, "piece", 10, 5, 1, 5000, 5, false)]
    [InlineData(QuantityMode.PACKAGE, "L", 50, 5, 4, 1000, 20, false)]
    public void Every_quantity_mode_keeps_a_stock_limited_partial_candidate(
        QuantityMode mode, string unit, decimal required, int availablePackages, decimal packageSize,
        decimal unitPrice, decimal expectedContribution, bool expectedFullCoverage)
    {
        var request = Request(required, unit, Paint);
        request.MaximumBudget = 100000;
        var listing = mode == QuantityMode.CONTINUOUS
            ? new Listing { SellerId = Guid.NewGuid(), CategoryId = Category, ConstructionItemTemplateId = Paint,
                QuantityMode = mode, BaseUnit = unit, Unit = unit, Quantity = availablePackages,
                UnitPrice = unitPrice, Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(2) }
            : Packaged(unit, packageSize, availablePackages, mode == QuantityMode.PIECE ? "PIECE" : "CAN", Paint, mode);
        listing.UnitPrice = unitPrice;

        var contribution = QuantitySemantics.EffectiveAffordableQuantity(request, listing);
        Assert.Equal(expectedContribution, contribution);
        Assert.Equal(expectedFullCoverage, contribution >= required);
        Assert.Null(MatchService.EligibilityReason(request, listing));
    }

    [Fact]
    public void Doors_piece_partial_with_route_cost_is_valid_and_priced_for_five_not_ten()
    {
        var request = Request(10, "piece", Paint);
        request.MaximumBudget = 30_000;
        var doors = Packaged("piece", 1, 5, "PIECE", Paint, QuantityMode.PIECE);
        doors.UnitPrice = 5_000;

        Assert.Equal(5, QuantitySemantics.EffectiveAffordableQuantity(request, doors, transportCost: 5_000));
        Assert.Equal(25_000, QuantitySemantics.MaterialCost(request, doors, transportCost: 5_000));
        Assert.Null(MatchService.EligibilityReason(request, doors));
    }

    [Fact]
    public void Budget_and_route_reduce_contribution_or_reject_when_no_minimum_increment_fits()
    {
        var request = Request(10, "piece", Paint);
        var listing = Packaged("piece", 1, 5, "PIECE", Paint, QuantityMode.PIECE);
        listing.UnitPrice = 5_000;
        request.MaximumBudget = 16_000;
        Assert.Equal(3, QuantitySemantics.EffectiveAffordableQuantity(request, listing, transportCost: 1_000));

        request.MaximumBudget = 5_999;
        Assert.Equal(0, QuantitySemantics.EffectiveAffordableQuantity(request, listing, transportCost: 1_000));
    }

    [Theory]
    [InlineData("L", 8, "CAN", 2, 4, "L", 8)]
    [InlineData("kg", 350, "BAG", 7, 50, "kg", 350)]
    [InlineData("ml", 900, "CARTRIDGE", 3, 300, "ml", 900)]
    public void Base_and_physical_package_requirement_inputs_normalize_equivalently(
        string baseUnit, decimal baseQuantity, string packageLabel, decimal count, decimal packageSize,
        string packageBaseUnit, decimal expected)
    {
        Assert.True(QuantitySemantics.TryNormalizeRequirement(baseQuantity, baseUnit, "BASE_QUANTITY", null, null, baseUnit, out var byBase));
        Assert.True(QuantitySemantics.TryNormalizeRequirement(count, packageLabel, "PACKAGE_COUNT", packageSize, packageBaseUnit, baseUnit, out var byPackage));
        Assert.Equal(expected, byBase.RequiredBaseQuantity);
        Assert.Equal(byBase.RequiredBaseQuantity, byPackage.RequiredBaseQuantity);
    }

    [Fact]
    public void Package_requirement_without_size_is_not_guessed()
    {
        Assert.False(QuantitySemantics.TryNormalizeRequirement(2, "BOX", "PACKAGE_COUNT", null, null, "kg", out _));
    }

    private static BuyerRequest Request(decimal quantity, string unit, Guid template) => new()
    {
        BuyerId = Guid.NewGuid(), CategoryId = Category, ConstructionItemTemplateId = template,
        RequiredQuantity = quantity, Unit = unit, MaximumBudget = 100000, Deadline = DateTime.UtcNow.AddDays(1)
    };

    private static Listing Packaged(string unit, decimal size, int count, string packageType, Guid template, QuantityMode mode = QuantityMode.PACKAGE) => new()
    {
        SellerId = Guid.NewGuid(), CategoryId = Category, ConstructionItemTemplateId = template,
        QuantityMode = mode, BaseUnit = unit, Unit = unit, PackageType = Enum.Parse<PackageType>(packageType),
        PackageSize = size, PackageCount = count, Quantity = size * count, Status = ListingStatus.ACTIVE,
        AvailableUntil = DateTime.UtcNow.AddDays(2), UnitPrice = 1
    };
}
