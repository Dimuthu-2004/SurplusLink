using System.Text.RegularExpressions;

namespace SurplusLink.Api.Auth;

public static partial class SriLankanContact
{
    [GeneratedRegex(@"^\d{9}[VX]$|^\d{12}$")]
    private static partial Regex NicPattern();
    [GeneratedRegex(@"^(?:0?94|\+94|0)7\d{8}$")]
    private static partial Regex PhonePattern();

    public static bool TryNormalizeNic(string? value, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim().Replace(" ", string.Empty).ToUpperInvariant();
        return NicPattern().IsMatch(normalized);
    }

    public static bool TryNormalizePhone(string? value, out string normalized)
    {
        var raw = (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        normalized = string.Empty;
        if (!PhonePattern().IsMatch(raw)) return false;
        normalized = raw.StartsWith("0", StringComparison.Ordinal) ? "+94" + raw[1..] : raw.StartsWith("+94", StringComparison.Ordinal) ? raw : "+" + raw;
        return normalized.Length == 12;
    }
}
