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
        (request.ConstructionItemTemplateId is null || listing.ConstructionItemTemplateId is null ||
         request.ConstructionItemTemplateId == listing.ConstructionItemTemplateId) &&
        TryConvert(request.RequiredQuantity, request.Unit, ListingBaseUnit(listing), out _);

    public static string? IncompatibilityReason(BuyerRequest request, Listing listing)
    {
        if (request.ConstructionItemTemplateId is not null && listing.ConstructionItemTemplateId is not null &&
            request.ConstructionItemTemplateId != listing.ConstructionItemTemplateId)
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

    public static decimal MaterialCost(BuyerRequest request, Listing listing)
    {
        if (!TryRequiredBaseQuantity(request, listing, out var requiredBase)) return decimal.MaxValue;
        var normalized = FromListing(listing);
        var packages = RequiredPackageCount(requiredBase, normalized);
        return packages > 0 ? packages * listing.UnitPrice : requiredBase * listing.UnitPrice;
    }
}
