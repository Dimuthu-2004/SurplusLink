using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

/// <summary>
/// The only quantity contract used at a marketplace boundary.  A package name
/// describes the physical selling unit; <see cref="BaseUnit"/> describes the
/// measurement which may be compared with a buyer requirement.
/// </summary>
public sealed record NormalizedQuantity(
    string BaseUnit,
    decimal AvailableBaseQuantity,
    QuantityMode QuantityMode,
    string? PackageType,
    decimal? PackageSize,
    int? AvailablePackageCount,
    decimal MinimumSellableIncrement,
    int DecimalPrecision);

public sealed record NormalizedRequirement(
    decimal RequiredBaseQuantity, string BaseUnit, string InputMode,
    decimal EnteredQuantity, string EnteredUnit, decimal? PreferredPackageSize,
    string? PackageBaseUnit);

public static class QuantitySemantics
{
    // Quantities and increments must already be in the requirement's unit.
    public static bool IsMinimalFulfillment(decimal required,
        IEnumerable<(decimal Quantity, decimal? PackageIncrement)> allocations)
    {
        var rows = allocations.ToArray();
        var total = rows.Sum(x => x.Quantity);
        if (total <= required) return true;
        return rows.All(x => x.PackageIncrement is > 0 &&
            total - x.PackageIncrement.Value < required);
    }

    private sealed record Unit(string Dimension, decimal Factor, int Precision);

    // Factors are expressed in a canonical unit per dimension.  This registry
    // intentionally contains only safe measurement conversions--never a
    // package label such as CAN/BAG/BOX and never material-density guesses.
    private static readonly IReadOnlyDictionary<string, Unit> Units = new Dictionary<string, Unit>(StringComparer.Ordinal)
    {
        ["l"] = new("volume", 1m, 3), ["ml"] = new("volume", .001m, 0), ["m3"] = new("volume", 1000m, 3),
        ["kg"] = new("mass", 1m, 3), ["g"] = new("mass", .001m, 0), ["tonne"] = new("mass", 1000m, 3),
        ["m"] = new("length", 1m, 3), ["cm"] = new("length", .01m, 1), ["mm"] = new("length", .001m, 0),
        ["m2"] = new("area", 1m, 3), ["cm2"] = new("area", .0001m, 0),
        ["piece"] = new("count", 1m, 0), ["set"] = new("set", 1m, 0), ["sheet"] = new("sheet", 1m, 0)
    };

    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["litre"] = "l", ["litres"] = "l", ["liter"] = "l", ["liters"] = "l",
        ["cubic metre"] = "m3", ["cubic metres"] = "m3", ["cube"] = "m3", ["m³"] = "m3",
        ["square metre"] = "m2", ["square metres"] = "m2", ["sqm"] = "m2", ["m²"] = "m2",
        ["pcs"] = "piece", ["pieces"] = "piece", ["unit"] = "piece", ["units"] = "piece",
        ["ton"] = "tonne", ["tons"] = "tonne", ["tonnes"] = "tonne"
    };

    public static string CanonicalUnit(string unit)
    {
        var normalized = MaterialUnits.Normalize(unit).Replace(" ", string.Empty, StringComparison.Ordinal);
        return Aliases.TryGetValue(normalized, out var canonical) ? canonical : normalized;
    }

    public static bool TryConvert(decimal quantity, string fromUnit, string toUnit, out decimal converted)
    {
        converted = 0;
        var from = CanonicalUnit(fromUnit);
        var to = CanonicalUnit(toUnit);
        if (!Units.TryGetValue(from, out var fromDefinition) || !Units.TryGetValue(to, out var toDefinition) ||
            fromDefinition.Dimension != toDefinition.Dimension)
            return false;
        converted = quantity * fromDefinition.Factor / toDefinition.Factor;
        return true;
    }

    public static string[] ResolveBuyerInputModes(IEnumerable<string>? configured, string? quantityMode)
    {
        var modes = configured?.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (modes.Length > 0) return modes;
        return quantityMode?.ToUpperInvariant() switch
        {
            "PACKAGE" => ["BASE_QUANTITY", "PACKAGE_COUNT"],
            "PIECE" => ["PIECE_COUNT"],
            _ => ["CONTINUOUS_QUANTITY"]
        };
    }

    public static bool TryNormalizeRequirement(decimal enteredQuantity, string enteredUnit, string? inputMode,
        decimal? preferredPackageSize, string? packageBaseUnit, string canonicalBaseUnit,
        out NormalizedRequirement normalized)
    {
        normalized = default!;
        if (enteredQuantity <= 0 || string.IsNullOrWhiteSpace(enteredUnit)) return false;
        var mode = string.IsNullOrWhiteSpace(inputMode) ? "BASE_QUANTITY" : inputMode.Trim().ToUpperInvariant();
        var baseUnit = CanonicalUnit(canonicalBaseUnit);
        if (mode == "PACKAGE_COUNT")
        {
            if (decimal.Truncate(enteredQuantity) != enteredQuantity || preferredPackageSize is not > 0 ||
                string.IsNullOrWhiteSpace(packageBaseUnit) ||
                !TryConvert(enteredQuantity * preferredPackageSize.Value, packageBaseUnit, baseUnit, out var packageBase))
                return false;
            normalized = new(packageBase, baseUnit, mode, enteredQuantity, enteredUnit.Trim(), preferredPackageSize,
                CanonicalUnit(packageBaseUnit));
            return true;
        }
        if (!TryConvert(enteredQuantity, enteredUnit, baseUnit, out var converted)) return false;
        if (mode == "PIECE_COUNT" && decimal.Truncate(converted) != converted) return false;
        normalized = new(converted, baseUnit, mode, enteredQuantity, CanonicalUnit(enteredUnit), null, null);
        return true;
    }

    public static bool IsCompatible(BuyerRequest request, Listing listing) =>
        IncompatibilityReason(request, listing) is null;

    public static string? IncompatibilityReason(BuyerRequest request, Listing listing)
    {
        if (!Matching.ItemRelevance.Evaluate(request, listing).Eligible)
            return "ITEM_MISMATCH";
        return TryConvert(request.RequiredQuantity, request.Unit, ListingBaseUnit(listing), out _) ? null : "UNIT_MISMATCH";
    }

    public static string ListingBaseUnit(Listing listing) => CanonicalUnit(listing.BaseUnit ?? listing.Unit);

    public static bool TryRequiredBaseQuantity(BuyerRequest request, Listing listing, out decimal quantity) =>
        TryConvert(request.RequiredQuantity, request.Unit, ListingBaseUnit(listing), out quantity);

    public static NormalizedQuantity FromListing(Listing listing)
    {
        var baseUnit = ListingBaseUnit(listing);
        var availableBase = Math.Max(0, listing.Quantity - listing.ReservedQuantity);
        var packaged = listing.QuantityMode is QuantityMode.PACKAGE or QuantityMode.PIECE;
        int? availablePackages = packaged && listing.PackageCount.HasValue
            ? Math.Max(0, listing.PackageCount.Value - listing.ReservedPackageCount) : null;
        decimal? packageSize = packaged ? listing.PackageSize ?? 1m : null;
        // Quantity has been base-equivalent since the package-aware migration.
        // Recalculate from physical count when possible so stale decimal fields
        // cannot create a fractional physical inventory.
        if (availablePackages.HasValue && packageSize is > 0)
            availableBase = availablePackages.Value * packageSize.Value;
        var precision = Units.TryGetValue(baseUnit, out var definition) ? definition.Precision : 3;
        return new(baseUnit, availableBase, listing.QuantityMode, listing.PackageType?.ToString(), packageSize,
            availablePackages, packageSize ?? (precision == 0 ? 1m : .001m), precision);
    }

    public static decimal RequiredPackageCount(decimal requiredBaseQuantity, NormalizedQuantity quantity) =>
        quantity.QuantityMode is QuantityMode.PACKAGE or QuantityMode.PIECE && quantity.PackageSize is > 0
            ? decimal.Ceiling(requiredBaseQuantity / quantity.PackageSize.Value) : 0;

    /// <summary>
    /// Returns the largest physical contribution this listing can make to this
    /// requirement. It is candidate metadata only: allocation remains an
    /// explicit buyer action. When routing is known, transport is included in
    /// the same budget envelope before determining the contribution.
    /// </summary>
    public static decimal EffectiveAffordableQuantity(BuyerRequest request, Listing listing, decimal? transportCost = null)
    {
        if (!TryRequiredBaseQuantity(request, listing, out var requiredBase)) return 0;
        var normalized = FromListing(listing);
        var maxPossibleBase = Math.Min(normalized.AvailableBaseQuantity, requiredBase);
        if (maxPossibleBase <= 0) return 0;

        var availableBudget = Math.Max(0, request.MaximumBudget - Math.Max(0, transportCost ?? 0));
        if (normalized.QuantityMode is QuantityMode.PACKAGE or QuantityMode.PIECE && normalized.PackageSize is > 0)
        {
            var maxPossiblePackages = (int)Math.Floor(maxPossibleBase / normalized.PackageSize.Value);
            var maxAffordablePackages = listing.UnitPrice > 0 ? (int)Math.Floor(availableBudget / listing.UnitPrice) : maxPossiblePackages;
            var effectivePackages = Math.Min(maxPossiblePackages, maxAffordablePackages);
            return effectivePackages >= 1 ? effectivePackages * normalized.PackageSize.Value : 0;
        }
        else
        {
            var maxAffordableBase = listing.UnitPrice > 0 ? availableBudget / listing.UnitPrice : maxPossibleBase;
            var effectiveBase = Math.Min(maxPossibleBase, maxAffordableBase);
            var minIncrement = normalized.MinimumSellableIncrement;
            // Continuous inventory may be decimal, but it must still be sold
            // in complete configured increments.
            effectiveBase = decimal.Floor(effectiveBase / minIncrement) * minIncrement;
            return effectiveBase >= minIncrement ? effectiveBase : 0;
        }
    }

    public static decimal MaterialCost(BuyerRequest request, Listing listing, decimal? transportCost = null)
    {
        if (!TryRequiredBaseQuantity(request, listing, out var requiredBase)) return decimal.MaxValue;
        var effectiveQty = EffectiveAffordableQuantity(request, listing, transportCost);
        if (effectiveQty <= 0) return decimal.MaxValue;
        var normalized = FromListing(listing);
        if (normalized.QuantityMode is QuantityMode.PACKAGE or QuantityMode.PIECE && normalized.PackageSize is > 0)
        {
            var packages = (int)Math.Floor(effectiveQty / normalized.PackageSize.Value);
            return packages * listing.UnitPrice;
        }
        return effectiveQty * listing.UnitPrice;
    }
}
