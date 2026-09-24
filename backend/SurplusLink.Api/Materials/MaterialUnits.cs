using System.Text.RegularExpressions;

namespace SurplusLink.Api.Materials;

public static class MaterialUnits
{
    public static string Normalize(string unit) => Regex.Replace(unit.Trim().ToLowerInvariant(), @"\s+", " ");
    public static IReadOnlyList<string> Distinct(IEnumerable<string> units) => units.Select(Normalize)
        .Where(unit => unit.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(unit => unit, StringComparer.Ordinal).ToArray();
}
