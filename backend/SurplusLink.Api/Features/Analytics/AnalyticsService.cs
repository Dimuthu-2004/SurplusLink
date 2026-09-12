using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Features.Analytics;

public sealed class AnalyticsService(SurplusLinkDbContext dbContext) : IAnalyticsService
{
    public async Task<ListingAnalyticsSummary> GetListingSummaryAsync(
        CancellationToken cancellationToken)
    {
        var activeListings = await dbContext.Listings
            .AsNoTracking()
            .CountAsync(listing => listing.Status == ListingStatus.AVAILABLE, cancellationToken);

        var categoryTotals = await dbContext.Listings
            .AsNoTracking()
            .GroupBy(listing => new { listing.CategoryId, listing.Category.Name })
            .Select(group => new CategoryListingTotal(
                group.Key.CategoryId,
                group.Key.Name,
                group.Count(),
                group.Sum(listing => listing.Quantity)))
            .OrderByDescending(total => total.ListingCount)
            .ThenBy(total => total.CategoryName)
            .ToListAsync(cancellationToken);

        return new ListingAnalyticsSummary(activeListings, categoryTotals);
    }

    public async Task<RequirementAnalyticsSummary> GetRequirementSummaryAsync(
        CancellationToken cancellationToken)
    {
        var openRequirements = await dbContext.MaterialRequests
            .AsNoTracking()
            .CountAsync(request => request.Status == MaterialRequestStatus.OPEN, cancellationToken);
        var pendingRequirements = await dbContext.MaterialRequests
            .AsNoTracking()
            .CountAsync(
                request => request.Status == MaterialRequestStatus.PENDING_APPROVAL,
                cancellationToken);

        var now = DateTime.UtcNow;
        var upcomingDeadlines = await dbContext.MaterialRequests
            .AsNoTracking()
            .Where(request =>
                request.DeadlineUtc >= now &&
                (request.Status == MaterialRequestStatus.OPEN ||
                 request.Status == MaterialRequestStatus.MATCHED ||
                 request.Status == MaterialRequestStatus.PENDING_APPROVAL))
            .OrderBy(request => request.DeadlineUtc)
            .Take(10)
            .Select(request => new UpcomingRequirement(
                request.Id,
                request.Title,
                request.Category.Name,
                request.DeadlineUtc,
                request.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new RequirementAnalyticsSummary(
            openRequirements,
            pendingRequirements,
            upcomingDeadlines);
    }

    public async Task<MatchAnalyticsSummary> GetMatchSummaryAsync(
        CancellationToken cancellationToken)
    {
        var averageScore = await dbContext.Matches
            .AsNoTracking()
            .Select(match => (decimal?)match.Score)
            .AverageAsync(cancellationToken);
        var averageDistanceKm = await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.DistanceKm != null)
            .Select(match => match.DistanceKm)
            .AverageAsync(cancellationToken);
        var topRejectionReasons = await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.RejectionReason != null && match.RejectionReason != string.Empty)
            .GroupBy(match => match.RejectionReason!)
            .Select(group => new RejectionReasonTotal(group.Key, group.Count()))
            .OrderByDescending(total => total.Count)
            .ThenBy(total => total.Reason)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new MatchAnalyticsSummary(
            averageScore,
            averageDistanceKm,
            topRejectionReasons);
    }

    public async Task<TransactionAnalyticsSummary> GetTransactionSummaryAsync(
        CancellationToken cancellationToken)
    {
        var counts = await dbContext.Transactions
            .AsNoTracking()
            .GroupBy(transaction => transaction.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return new TransactionAnalyticsSummary(
            GetCount(TransactionStatus.PENDING_APPROVAL),
            GetCount(TransactionStatus.APPROVED),
            GetCount(TransactionStatus.REJECTED),
            GetCount(TransactionStatus.COMPLETED));

        int GetCount(TransactionStatus status) => counts.GetValueOrDefault(status);
    }
}
