namespace SurplusLink.Api.Models;

public sealed class ListingPhoto : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public string PhotoUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Listing Listing { get; set; } = null!;
}
