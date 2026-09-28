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

    // Legacy decimal fields remain base-equivalent values. Package stock is
    // deliberately tracked as whole sellable units alongside them.
    public QuantityMode QuantityMode { get; set; } = QuantityMode.LEGACY;
    public string? BaseUnit { get; set; }
    public PackageType? PackageType { get; set; }
    public decimal? PackageSize { get; set; }
    public int? PackageCount { get; set; }
    public int ReservedPackageCount { get; set; }

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

public enum QuantityMode { LEGACY, PACKAGE, PIECE, CONTINUOUS }
public enum PackageType { CAN, BAG, BOX, CARTRIDGE, ROLL, SHEET, ROD, PIPE, PACK, PIECE, OTHER }
