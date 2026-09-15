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

public sealed class MaterialListingQuery
{
    [MaxLength(200)]
    public string? Search { get; init; }

    [MaxLength(120)]
    public string? Category { get; init; }

    [MaxLength(24)]
    public string? Status { get; init; }

    [MaxLength(24)]
    public string? Condition { get; init; }

    [Range(typeof(decimal), "0", "999999999999999.99")]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", "999999999999999.99")]
    public decimal? MaxPrice { get; init; }

    [RegularExpression("^(unitPrice|quantity|createdAt|availableUntil)$", ErrorMessage = "SortBy must be unitPrice, quantity, createdAt, or availableUntil.")]
    public string SortBy { get; init; } = "createdAt";

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be asc or desc.")]
    public string SortDir { get; init; } = "desc";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record PagedMaterialListingsResponse(
    IReadOnlyList<MaterialListingResponse> Items,
    int TotalCount,
    int TotalPages,
    int Page,
    int PageSize);

public sealed record MaterialListingHistoryResponse(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    DateTime CreatedAtUtc);

public sealed class MaterialAnalyticsQuery
{
    [Range(1, 90)]
    public int ExpiringWithinDays { get; init; } = 7;

    [Range(typeof(decimal), "0.001", "100")]
    public decimal LowRemainingPercent { get; init; } = 10m;
}

public sealed record MaterialAnalyticsCountResponse(string Key, int Count);

public sealed record MaterialAnalyticsSummaryResponse(
    int ActiveCount,
    IReadOnlyList<MaterialAnalyticsCountResponse> ListingsByCategory,
    IReadOnlyList<MaterialAnalyticsCountResponse> ListingsByStatus,
    IReadOnlyList<MaterialListingResponse> ExpiringListings,
    IReadOnlyList<MaterialListingResponse> LowRemainingQuantityListings);

