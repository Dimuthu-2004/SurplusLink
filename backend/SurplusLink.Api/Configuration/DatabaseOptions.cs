namespace SurplusLink.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string? SurplusLink { get; set; }
}
