namespace SurplusLink.Api.Features.Analytics;

public sealed record CategoryListingTotal(
    Guid CategoryId,
    string CategoryName,
    int ListingCount,
    decimal TotalQuantity);

public sealed record ListingAnalyticsSummary(
    int ActiveListings,
    IReadOnlyList<CategoryListingTotal> CategoryTotals);

public sealed record UpcomingRequirement(
    Guid Id,
    string Title,
    string CategoryName,
    DateTime DeadlineUtc,
    string Status);

public sealed record RequirementAnalyticsSummary(
    int OpenRequirements,
    int PendingRequirements,
    IReadOnlyList<UpcomingRequirement> UpcomingDeadlines);

public sealed record RejectionReasonTotal(string Reason, int Count);

public sealed record MatchAnalyticsSummary(
    decimal? AverageScore,
    decimal? AverageDistanceKm,
    IReadOnlyList<RejectionReasonTotal> TopRejectionReasons);

public sealed record TransactionAnalyticsSummary(
    int PendingApprovals,
    int ApprovedTransactions,
    int RejectedTransactions,
    int CompletedTransactions);
