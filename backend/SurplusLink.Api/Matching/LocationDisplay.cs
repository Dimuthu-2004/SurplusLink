namespace SurplusLink.Api.Matching;

internal static class LocationDisplay
{
    // Stored address is already user-entered.  Return a compact locality rather
    // than repeatedly reverse-geocoding device coordinates during list refreshes.
    internal static string? Concise(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        if (parts.Length == 1) return parts[0];
        var candidate = parts[^1];
        if (candidate.Equals("Sri Lanka", StringComparison.OrdinalIgnoreCase) && parts.Length > 1) candidate = parts[^2];
        return candidate.Length <= 80 ? candidate : candidate[..80].TrimEnd();
    }
}
