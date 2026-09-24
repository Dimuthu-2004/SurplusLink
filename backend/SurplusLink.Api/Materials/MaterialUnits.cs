using System.Text.RegularExpressions;

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
}
