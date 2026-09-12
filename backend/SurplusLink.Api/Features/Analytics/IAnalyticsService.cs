namespace SurplusLink.Api.Features.Analytics;

public interface IAnalyticsService
{
    Task<ListingAnalyticsSummary> GetListingSummaryAsync(CancellationToken cancellationToken);

    Task<RequirementAnalyticsSummary> GetRequirementSummaryAsync(CancellationToken cancellationToken);

    Task<MatchAnalyticsSummary> GetMatchSummaryAsync(CancellationToken cancellationToken);

    Task<TransactionAnalyticsSummary> GetTransactionSummaryAsync(CancellationToken cancellationToken);
}
