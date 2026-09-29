using System.Text.RegularExpressions;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public static class MaterialUnits
{
    public static readonly IReadOnlyList<string> Catalog = Array.AsReadOnly(new[]
        { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

    public static string[] ValidateAllowed(IEnumerable<string> units, IEnumerable<string> catalog)
    {
        if (units is null || units.Any(string.IsNullOrWhiteSpace))
            throw new MaterialOperationException(MaterialOperationError.Validation, "Select valid units from the unit catalog.");
        var normalized = Distinct(units);
        if (normalized.Count == 0 || normalized.Any(unit => !catalog.Contains(unit)))
            throw new MaterialOperationException(MaterialOperationError.Validation, "Select one or more units from the unit catalog.");
        return normalized.ToArray();
    }

    public static string Normalize(string unit) => Regex.Replace(unit.Trim().ToLowerInvariant(), @"\s+", " ");
    public static IReadOnlyList<string> Distinct(IEnumerable<string> units) => units.Select(Normalize)
        .Where(unit => unit.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(unit => unit, StringComparer.Ordinal).ToArray();

    public sealed record UnitDefinition(string Code, string DisplayName, string MeasurementType,
        QuantityMode QuantityMode, decimal AllowedStep, int DecimalPrecision);

    // A measurement unit does not determine the sellable form. For example L
    // may be loose continuous stock or the base unit of a sealed can.
    public static readonly IReadOnlyList<UnitDefinition> Definitions = Array.AsReadOnly(new[]
    {
        new UnitDefinition("pcs", "pieces", "count", QuantityMode.PIECE, 1, 0),
        new UnitDefinition("m", "metres", "length", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("m2", "square metres", "area", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("m3", "cubic metres", "volume", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("kg", "kilograms", "mass", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("g", "grams", "mass", QuantityMode.CONTINUOUS, 1, 0),
        new UnitDefinition("l", "litres", "volume", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("ml", "millilitres", "volume", QuantityMode.CONTINUOUS, 1, 0),
        new UnitDefinition("bag", "bag", "package", QuantityMode.PACKAGE, 1, 0),
        new UnitDefinition("box", "box", "package", QuantityMode.PACKAGE, 1, 0),
        new UnitDefinition("set", "set", "count", QuantityMode.PIECE, 1, 0),
        new UnitDefinition("roll", "roll", "package", QuantityMode.PACKAGE, 1, 0),
        new UnitDefinition("sheet", "sheet", "count", QuantityMode.PIECE, 1, 0),
        new UnitDefinition("tonne", "tonne", "mass", QuantityMode.CONTINUOUS, .001m, 3),
        new UnitDefinition("pair", "pair", "count", QuantityMode.PIECE, 1, 0)
    });

    // Continuous-measurement units (length, area, volume, mass, liquid).
    // Every unit in the catalog that is NOT in this set is considered discrete
    // (count-based: pcs, bag, box, set, roll, sheet, tonne, pair, …).
    private static readonly HashSet<string> _continuousUnits =
        new(["m", "m2", "m3", "kg", "g", "l"], StringComparer.Ordinal);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="unit"/> is a discrete / count-based
    /// unit (e.g. pcs, bag, tonne), <c>false</c> when it is a continuous measurement
    /// unit (e.g. m, kg, l).  The comparison is case-insensitive and ignores
    /// surrounding whitespace.
    /// </summary>
    public static bool IsDiscrete(string unit) => !_continuousUnits.Contains(Normalize(unit));
}
