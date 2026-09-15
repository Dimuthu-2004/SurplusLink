using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Materials;

public class CreateMaterialListingRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(2_000)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; init; }

    [Required, MaxLength(32)]
    public string Unit { get; init; } = string.Empty;

    [Required, RegularExpression("^(NEW|EXCELLENT|GOOD|FAIR|POOR)$")]
    public string Condition { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999999999.99")]
    public decimal UnitPrice { get; init; }

    [Range(-90d, 90d)]
    public decimal? Latitude { get; init; }

    [Range(-180d, 180d)]
    public decimal? Longitude { get; init; }

    [Required]
    public DateTime? AvailableUntil { get; init; }

    [MaxLength(10)]
    public IReadOnlyList<ListingPhotoRequest> Photos { get; init; } = Array.Empty<ListingPhotoRequest>();
}

public sealed class UpdateMaterialListingRequest : CreateMaterialListingRequest;

public sealed class ListingPhotoRequest
{
    [Required, Url, MaxLength(2_048)]
    public string PhotoUrl { get; init; } = string.Empty;

    [Range(0, 100)]
    public int SortOrder { get; init; }
}

public sealed class VerifyListingRequest
{
    public bool Approved { get; init; }
}

public sealed record ListingPhotoResponse(Guid Id, string PhotoUrl, int SortOrder);

public sealed record MaterialListingResponse(
    Guid Id,
    Guid SellerId,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string Description,
    decimal Quantity,
    decimal ReservedQuantity,
    string Unit,
    string Condition,
    decimal UnitPrice,
    decimal? Latitude,
    decimal? Longitude,
    DateTime AvailableUntil,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ListingPhotoResponse> Photos);

public sealed class MaterialCategoryRequest
{
    [Required, MaxLength(120)]
    public string Name { get; init; } = string.Empty;
}

public sealed record MaterialCategoryResponse(Guid Id, string Name, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

