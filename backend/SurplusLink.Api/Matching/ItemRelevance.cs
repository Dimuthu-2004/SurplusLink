using System.Text.RegularExpressions;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

public sealed record ItemRelevanceResult(string Classification, decimal Confidence, string ReasonCode, string[] Evidence)
{
    public bool Eligible => Classification == "SAME_ITEM" && Confidence >= .95m;
}

/// <summary>Conservative identity gate. Category and logistics never establish item identity.</summary>
public static class ItemRelevance
{
    private static readonly ConstructionItemTemplate[] Catalog = ConstructionItemTemplateCatalogSeed.GetTemplates();
    public static ItemRelevanceResult Evaluate(BuyerRequest request, Listing listing)
    {
        if (request.ConstructionItemTemplateId is { } requested && listing.ConstructionItemTemplateId is { } offered)
            return requested == offered ? new("SAME_ITEM", 1, "EXACT_TEMPLATE", [requested.ToString()])
                : new("INCOMPATIBLE", 1, "DIFFERENT_TEMPLATE", []);
        var template = request.ConstructionItemTemplate ?? Catalog.FirstOrDefault(x => x.Id == request.ConstructionItemTemplateId);
        var name = template?.Name ?? request.Title;
        var aliases = template?.Aliases is { Length: > 0 } configured ? configured : [name];
        var title = Normalize(listing.Title);
        var evidence = aliases.Where(x => !IsBroad(x) && Contains(title, x)).ToArray();
        // A canonical competing item name in the title makes custom identity ambiguous.
        var competitors = Catalog.Where(x => x.Id != template?.Id && !IsBroad(x.Name) && Contains(title, x.Name)).ToArray();
        if (evidence.Length == 0 || competitors.Length > 0)
            return new("INCOMPATIBLE", 1, "ITEM_IDENTITY_NOT_ESTABLISHED", []);
        // Match a complete item name, never a broad word from category or description.
        return new("SAME_ITEM", .96m, "CUSTOM_ITEM_NAME_MATCH", evidence);
    }
    private static bool Contains(string text, string phrase) => text.Contains(" " + Normalize(phrase).Trim() + " ", StringComparison.Ordinal);
    private static string Normalize(string text) => " " + Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim() + " ";
    private static bool IsBroad(string name) => string.IsNullOrWhiteSpace(name) ||
        new[] { "equipment", "finishes", "construction", "material", "materials", "other", "custom item", "miscellaneous construction surplus" }
            .Contains(name.Trim().ToLowerInvariant()) || name.Contains("requirement", StringComparison.OrdinalIgnoreCase);
}
