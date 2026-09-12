namespace SurplusLink.Api.Configuration;

public sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ConfiguredOrigins";

    public string[] AllowedOrigins { get; set; } = [];

    public static bool IsValidOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin)
            || origin.Contains('*', StringComparison.Ordinal)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        return string.Equals(
            origin,
            uri.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);
    }
}
