using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public static class TemplatePackageSize
{
    public static decimal? Resolve(ConstructionItemTemplate template, string? specifications, decimal? entered)
    {
        var fields = JsonNode.Parse(template.AttributeSchema) as JsonArray;
        var source = fields?.OfType<JsonObject>().FirstOrDefault(x => x["packageSizeSource"] is not null);
        if (source is null) return entered;
        JsonObject? values;
        try { values = JsonNode.Parse(specifications ?? "{}") as JsonObject; }
        catch { values = null; }
        decimal Number(string id)
        {
            var raw = values?[id]?.ToString() ?? "";
            var match = Regex.Match(raw, @"^\s*(\d+(?:\.\d+)?)");
            return decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
        var id = source["id"]!.GetValue<string>();
        var size = source["packageSizeSource"]!.GetValue<string>() == "CALCULATED"
            ? Number("widthMm") * Number("heightMm") * Number("piecesPerBox") / 1_000_000m
            : Number(id);
        if (size <= 0)
            throw new MaterialOperationException(MaterialOperationError.Validation,
                "Choose or enter the amount contained in one package.", $"specifications.{id}", "PACKAGE_SIZE_REQUIRED");
        if (source["packageSizeSource"]!.GetValue<string>() == "CALCULATED" && Number("piecesPerBox") % 1 != 0)
            throw new MaterialOperationException(MaterialOperationError.Validation,
                "Enter a whole number of items per package.", "specifications.piecesPerBox", "WHOLE_NUMBER_REQUIRED");
        return size;
    }
}
