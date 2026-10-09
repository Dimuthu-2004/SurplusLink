using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

internal static class MatchRecommendation
{
    internal static string? InvalidReason(BuyerRequest request, MaterialMatch match, DateTime now) =>
        match.Status != MatchStatus.ROUTED ? match.RejectionReason ?? match.Status.ToString()
        : request.Deadline <= now ? "REQUIREMENT_EXPIRED"
        : match.RejectionReason ?? MatchService.EligibilityReason(request, match.Listing)
        ?? (match.Listing.AvailableUntil.Date < request.Deadline.Date ? "LISTING_EXPIRES_BEFORE_DELIVERY" : null)
        ?? (match.Distance is null or < 0 || match.DurationMinutes is null or < 0 ||
            match.EstimatedTransportCost is null or < 0 ? "ROUTE_DATA_INCOMPLETE" : null)
        ?? (match.DurationMinutes > (decimal)(request.Deadline - now).TotalMinutes ? "DELIVERY_DEADLINE_EXCEEDED" : null)
        ?? (QuantitySemantics.MaterialCost(request, match.Listing, match.EstimatedTransportCost) + match.EstimatedTransportCost > request.MaximumBudget
            ? "TOTAL_COST_EXCEEDS_BUDGET" : null);

    internal static MaterialMatch? Choose(BuyerRequest request, IEnumerable<MaterialMatch> matches, DateTime now) =>
        matches.Where(x => InvalidReason(request, x, now) is null)
            .OrderByDescending(x => MatchScoring.Score(x.Listing.Condition.ToString(),
                QuantitySemantics.MaterialCost(request, x.Listing, x.EstimatedTransportCost), request.MaximumBudget,
                x.Distance, x.EstimatedTransportCost,
                PreferenceCompatibility.Evaluate(request, x.Listing).MatchedCount,
                PreferenceCompatibility.Evaluate(request, x.Listing).ConsideredCount))
            .ThenByDescending(x => MatchScoring.ConditionRank(x.Listing.Condition.ToString()))
            .ThenBy(x => QuantitySemantics.MaterialCost(request, x.Listing, x.EstimatedTransportCost) + x.EstimatedTransportCost)
            .ThenBy(x => x.Distance)
            .ThenBy(x => x.ListingId.ToString(), StringComparer.Ordinal)
            .ThenBy(x => x.Id.ToString(), StringComparer.Ordinal).FirstOrDefault();

    // Called in matching transactions and on reads to repair legacy recommendations
    // and invalidate recommendations whose stock/availability has since changed.
    internal static async Task<BuyerRequest> RefreshAsync(SurplusLinkDbContext db, Guid requirementId, CancellationToken ct)
    {
        var request = await db.BuyerRequests.SingleAsync(x => x.Id == requirementId, ct);
        var matches = await db.Matches.Include(x => x.Listing).ThenInclude(x => x.ConstructionItemTemplate)
            .Where(x => x.MaterialRequestId == requirementId).ToListAsync(ct);
        var now = DateTime.UtcNow;
        request.RecommendedMatchId = Choose(request, matches, now)?.Id;
        request.RecommendationReason = request.RecommendedMatchId.HasValue ? null
            : matches.Count == 0 ? "NO_CANDIDATES"
            : string.Join("; ", matches.OrderBy(x => x.Id).Select(x => $"{x.Id}:{InvalidReason(request, x, now)}"));
        await db.SaveChangesAsync(ct);
        return request;
    }
}
