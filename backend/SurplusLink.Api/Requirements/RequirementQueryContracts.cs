using System.ComponentModel.DataAnnotations;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Requirements;

public class RequirementPageQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed class RequirementQuery : RequirementPageQuery, IValidatableObject
{
    [StringLength(200)]
    public string? Search { get; init; }
    public string? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTimeOffset? DeadlineFrom { get; init; }
    public DateTimeOffset? DeadlineTo { get; init; }
    [Required, RegularExpression("(?i)^(deadline|budget|createdAt)$")]
    public string Sort { get; init; } = "createdAt";
    [Required, RegularExpression("(?i)^(asc|desc)$")]
    public string SortDir { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.GetNames<BuyerRequestStatus>().Contains(Status, StringComparer.OrdinalIgnoreCase))
            yield return new("Status must be a named requirement status.", [nameof(Status)]);
        if (CategoryId == Guid.Empty)
            yield return new("CategoryId must be a nonempty GUID.", [nameof(CategoryId)]);
        if (DeadlineFrom.HasValue && DeadlineTo.HasValue && DeadlineFrom > DeadlineTo)
            yield return new("DeadlineFrom must be before or equal to DeadlineTo.", [nameof(DeadlineFrom), nameof(DeadlineTo)]);
    }
}

public sealed record RequirementHistoryEntry(
    Guid Id, Guid? ActorUserId, string Action, string? FromStatus, string? ToStatus, DateTime CreatedAt)
{
    public static RequirementHistoryEntry From(AuditLog log)
    {
        var parts = log.Action.Split(':');
        return parts is ["STATUS_CHANGED", var from, var to]
            ? new(log.Id, log.ActorUserId, "STATUS_CHANGED", from, to, log.CreatedAtUtc)
            : new(log.Id, log.ActorUserId, log.Action, null, null, log.CreatedAtUtc);
    }
}

public sealed record RequirementHistoryPage(IReadOnlyList<RequirementHistoryEntry> Items, int Total, int Page, int PageSize);
public sealed record RequirementStatusCount(string Status, int Count);
public sealed record RequirementCategoryCount(Guid CategoryId, string CategoryName, int Count);
public sealed record RequirementQuantityAverage(string Unit, decimal AverageRequiredQuantity, int Count);
public sealed record RequirementAnalyticsSummary(
    int Total, IReadOnlyList<RequirementStatusCount> CountsByStatus,
    IReadOnlyList<RequirementCategoryCount> CountsByCategory, int OpenCount,
    DateTime AsOf, DateTime UpcomingUntil, int UpcomingDeadlineCount,
    IReadOnlyList<RequirementResponse> UpcomingDeadlines,
    decimal? AverageMaximumBudget, IReadOnlyList<RequirementQuantityAverage> AverageQuantityByUnit);
