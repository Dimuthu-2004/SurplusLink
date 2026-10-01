using System.Text.Json;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

/// <summary>
/// Compares buyer-selected template attributes with a listing without changing
/// the established condition/cost/distance score.  The template schema owns
/// the behaviour of each field; clients only render this structured result.
/// </summary>
public static class PreferenceCompatibility
{
    public static PreferenceCompatibilityResult Evaluate(BuyerRequest request, Listing listing)
    {
        if (string.IsNullOrWhiteSpace(request.BuyerPreferencesJson) ||
            listing.ConstructionItemTemplate is null)
            return PreferenceCompatibilityResult.Empty;

        try
        {
            using var requested = JsonDocument.Parse(request.BuyerPreferencesJson);
            using var offered = JsonDocument.Parse(string.IsNullOrWhiteSpace(listing.SpecificationsJson) ? "{}" : listing.SpecificationsJson);
            using var schema = JsonDocument.Parse(listing.ConstructionItemTemplate.AttributeSchema);
            if (requested.RootElement.ValueKind != JsonValueKind.Object ||
                offered.RootElement.ValueKind != JsonValueKind.Object ||
                schema.RootElement.ValueKind != JsonValueKind.Array)
                return PreferenceCompatibilityResult.Empty;

            var mismatches = new List<PreferenceMismatch>();
            var considered = 0;
            foreach (var field in schema.RootElement.EnumerateArray())
            {
                if (!field.TryGetProperty("id", out var idElement) || string.IsNullOrWhiteSpace(idElement.GetString()) ||
                    !field.TryGetProperty("buyerPreference", out var buyerPreference) || buyerPreference.ValueKind != JsonValueKind.True)
                    continue;
                var id = idElement.GetString()!;
                if (!requested.RootElement.TryGetProperty(id, out var requestedValue) || IsAny(requestedValue))
                    continue;

                considered++;
                var behavior = field.TryGetProperty("matchBehavior", out var configured)
                    ? configured.GetString()?.ToUpperInvariant() ?? "SOFT_PREFERENCE"
                    : "SOFT_PREFERENCE";
                if (behavior == "INFORMATIONAL") continue;
                var label = field.TryGetProperty("label", out var labelElement) && !string.IsNullOrWhiteSpace(labelElement.GetString())
                    ? labelElement.GetString()! : id;
                var wanted = Display(requestedValue);
                var hasSellerValue = offered.RootElement.TryGetProperty(id, out var sellerValue) && !IsBlank(sellerValue);
                if (!hasSellerValue || !Same(requestedValue, sellerValue))
                    mismatches.Add(new PreferenceMismatch(id, label, wanted,
                        hasSellerValue ? Display(sellerValue) : null, behavior));
            }
            var hard = mismatches.Any(x => x.Behavior == "HARD_REQUIREMENT");
            return new(hard ? "INCOMPATIBLE" : mismatches.Count == 0 ? "MATCHED" : "PARTIAL",
                Math.Max(0, considered - mismatches.Count), considered, mismatches, hard);
        }
        catch (JsonException)
        {
            // Existing malformed legacy JSON must not fabricate a preference match.
            return PreferenceCompatibilityResult.Empty;
        }
    }

    private static bool Same(JsonElement requested, JsonElement offered) =>
        string.Equals(Display(requested).Trim(), Display(offered).Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool IsBlank(JsonElement value) => value.ValueKind == JsonValueKind.Null ||
        value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString());
    private static bool IsAny(JsonElement value) => value.ValueKind == JsonValueKind.Null ||
        value.ValueKind == JsonValueKind.String && (string.IsNullOrWhiteSpace(value.GetString()) ||
            value.GetString()!.Equals("any", StringComparison.OrdinalIgnoreCase) ||
            value.GetString()!.Equals("no preference", StringComparison.OrdinalIgnoreCase));
    private static string Display(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
}

public sealed record PreferenceMismatch(string FieldKey, string Label, string RequestedValue, string? SellerValue, string Behavior);
public sealed record PreferenceCompatibilityResult(string Status, int MatchedCount, int ConsideredCount,
    IReadOnlyList<PreferenceMismatch> Mismatches, bool HasHardMismatch)
{
    public static readonly PreferenceCompatibilityResult Empty = new("NOT_SELECTED", 0, 0, [], false);
}
