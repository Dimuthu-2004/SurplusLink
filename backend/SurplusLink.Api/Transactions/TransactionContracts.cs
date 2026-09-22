using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Transactions;

public sealed class TransactionQuery : IValidatableObject
{
    public string? Status { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public Guid? UserId { get; set; }
    [Required, RegularExpression("(?i)^(createdAt|value|status)$")] public string SortBy { get; init; } = "createdAt";
    [Required, RegularExpression("(?i)^(asc|desc)$")] public string SortDir { get; init; } = "desc";
    [Range(1, 1_000_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.GetNames<TransactionStatus>().Contains(Status, StringComparer.OrdinalIgnoreCase))
            yield return new("Status must be a named transaction status.", [nameof(Status)]);
        if (CreatedFrom.HasValue && CreatedTo.HasValue && CreatedFrom > CreatedTo)
            yield return new("CreatedFrom must be before or equal to CreatedTo.", [nameof(CreatedFrom), nameof(CreatedTo)]);
        if (UserId == Guid.Empty) yield return new("UserId must be nonempty.", [nameof(UserId)]);
    }
}

public sealed class OfferQuery : IValidatableObject
{
    public string? Status { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public Guid? UserId { get; set; }
    [Required, RegularExpression("(?i)^(createdAt|value|status)$")] public string SortBy { get; init; } = "createdAt";
    [Required, RegularExpression("(?i)^(asc|desc)$")] public string SortDir { get; init; } = "desc";
    [Range(1, 1_000_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.GetNames<OfferStatus>().Contains(Status, StringComparer.OrdinalIgnoreCase))
            yield return new("Status must be a named offer status.", [nameof(Status)]);
        if (CreatedFrom.HasValue && CreatedTo.HasValue && CreatedFrom > CreatedTo)
            yield return new("CreatedFrom must be before or equal to CreatedTo.", [nameof(CreatedFrom), nameof(CreatedTo)]);
        if (UserId == Guid.Empty) yield return new("UserId must be nonempty.", [nameof(UserId)]);
    }
}

public sealed record OfferResponse(Guid Id, Guid MaterialMatchId, Guid BuyerId, Guid SellerId, decimal Quantity,
    decimal UnitValue, decimal TotalValue, [property: JsonConverter(typeof(JsonStringEnumConverter))] OfferStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TransactionResponse(Guid Id, Guid OfferId, Guid BuyerId, Guid SellerId, decimal Quantity,
    decimal TotalValue, decimal ReservedQuantity, [property: JsonConverter(typeof(JsonStringEnumConverter))] TransactionStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt);
public sealed record TransactionHistoryEntry(Guid Id, Guid? ActorUserId, string Action, DateTime CreatedAt, string? Note);
public sealed record TransactionHistoryPage(IReadOnlyList<TransactionHistoryEntry> Items, int Total, int TotalPages, int Page, int PageSize);
public sealed record TransactionAnalyticsSummary(int PendingApprovalCount, int ApprovedCount, int RejectedCount,
    decimal ReservedQuantity, int CompletionCount, decimal CompletedValue, double? CompletionRate);
public sealed class TransactionOperationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
