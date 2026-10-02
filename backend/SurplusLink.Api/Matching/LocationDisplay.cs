namespace SurplusLink.Api.Matching;

internal static class LocationDisplay
{
    private static readonly HashSet<string> SriLankanDistricts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ampara", "Anuradhapura", "Badulla", "Batticaloa", "Colombo", "Galle", "Gampaha", "Hambantota",
        "Jaffna", "Kalutara", "Kandy", "Kegalle", "Kilinochchi", "Kurunegala", "Mannar", "Matale", "Matara",
        "Monaragala", "Mullaitivu", "Nuwara Eliya", "Polonnaruwa", "Puttalam", "Ratnapura", "Trincomalee", "Vavuniya"
    };
    // Stored address is already user-entered.  Return a compact locality rather
    // than repeatedly reverse-geocoding device coordinates during list refreshes.
    internal static string? Concise(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        if (parts.Length == 1) return parts[0];
        var end = parts.Length - 1;
        if (parts[end].Equals("Sri Lanka", StringComparison.OrdinalIgnoreCase)) end--;
        // Sri Lankan user-entered addresses commonly end in "locality, district".
        // Prefer the locality while retaining the full address when that shape is
        // unavailable. This is deterministic presentation, not reverse geocoding.
        var candidate = end >= 1 && SriLankanDistricts.Contains(parts[end]) ? parts[end - 1] : parts[end];
        if (candidate.Any(char.IsDigit) || candidate.Length < 2)
            candidate = string.Join(", ", parts.Take(end + 1));
        return candidate.Length <= 80 ? candidate : candidate[..80].TrimEnd();
    }
}
