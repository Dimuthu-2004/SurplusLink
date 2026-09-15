namespace SurplusLink.Api.Models;

public sealed class Listing : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }

    public Guid CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public MaterialCondition Condition { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime AvailableUntil { get; set; }

    public ListingStatus Status { get; set; }

    public uint Version { get; set; }

    public User Seller { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public ICollection<ListingPhoto> Photos { get; } = new List<ListingPhoto>();
}
