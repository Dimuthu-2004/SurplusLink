using System.ComponentModel.DataAnnotations;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

public class MatchPageQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed class MatchQuery : MatchPageQuery, IValidatableObject
{
    public bool? Valid { get; init; }

    public bool? Rejected { get; init; }

    public string? Status { get; init; }

    [Required]
    [RegularExpression("(?i)^(score|distance|estimatedTransportCost|createdAt)$")]
    public string SortBy { get; init; } = "score";

    [Required]
    [RegularExpression("(?i)^(asc|desc)$")]
    public string SortDir { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Status is not null &&
            !Enum.GetNames<MatchStatus>()
                .Contains(
                    Status,
                    StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Status must be a named match status.",
                [nameof(Status)]);
        }
    }
}

public sealed record MatchResponse(
    Guid Id,
    Guid RequirementId,
    Guid ListingId,
    decimal Score,
    decimal? Distance,
    decimal? EstimatedTransportCost,
    string Status,
    bool Valid,
    bool Rejected,
    string? RejectionReason,
    DateTime CreatedAt,
    decimal? DurationMinutes = null,
    string? MaterialTitle = null,
    string? CategoryName = null,
    Guid? SellerId = null,
    decimal? Quantity = null,
    string? Unit = null,
    decimal? UnitPrice = null,
    DateTime? AvailableUntil = null,
    DateTime? RequiredBy = null,
    decimal? AvailableQuantity = null,
    decimal? MaximumBudget = null,
    string? RequirementStatus = null
);

public sealed record MatchPage(
    IReadOnlyList<MatchResponse> Items,
    int Total,
    int TotalPages,
    int Page,
    int PageSize
);

public sealed record MatchHistoryEntry(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string? Outcome,
    DateTime CreatedAt
);

public sealed record MatchHistoryPage(
    IReadOnlyList<MatchHistoryEntry> Items,
    int Total,
    int TotalPages,
    int Page,
    int PageSize
);

public sealed record RejectionReasonCount(
    string Reason,
    int Count
);

public sealed record MatchAnalyticsSummary(
    int Total,
    decimal? AverageScore,
    decimal? AverageDistance,
    IReadOnlyList<RejectionReasonCount> TopRejectionReasons,
    int RouteSuccessCount,
    int RouteFailureCount,
    double? RouteSuccessRate,
    double? RouteFailureRate,
    int ValidCount = 0,
    int RejectedCount = 0,
    decimal? AverageCost = null
);

public sealed class MatchException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}