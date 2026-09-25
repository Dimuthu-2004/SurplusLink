using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

public static class MatchQueryBuilder
{
    public static IQueryable<MaterialMatch> Filter(IQueryable<MaterialMatch> rows, MatchQuery query)
    {
        if (query.Valid.HasValue)
            // A candidate is selectable only after a successful route. Generated,
            // ranked and route-failed rows remain comparison history, not valid matches.
            rows = rows.Where(x => (x.Status == MatchStatus.ROUTED) == query.Valid.Value);
        if (query.Rejected.HasValue)
            rows = rows.Where(x => (x.Status == MatchStatus.REJECTED) == query.Rejected.Value);
        if (query.Status is not null)
        {
            var status = Enum.Parse<MatchStatus>(query.Status, true);
            rows = rows.Where(x => x.Status == status);
        }
        return rows;
    }

    public static IOrderedQueryable<MaterialMatch> Sort(IQueryable<MaterialMatch> rows, MatchQuery query)
    {
        var asc = query.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase);
        // Unknown route values always follow measured values in either direction.
        var ordered = query.SortBy.ToLowerInvariant() switch
        {
            "score" => asc ? rows.OrderBy(x => x.Score) : rows.OrderByDescending(x => x.Score),
            "distance" => asc
                ? rows.OrderBy(x => x.Distance == null).ThenBy(x => x.Distance)
                : rows.OrderBy(x => x.Distance == null).ThenByDescending(x => x.Distance),
            "estimatedtransportcost" => asc
                ? rows.OrderBy(x => x.EstimatedTransportCost == null).ThenBy(x => x.EstimatedTransportCost)
                : rows.OrderBy(x => x.EstimatedTransportCost == null).ThenByDescending(x => x.EstimatedTransportCost),
            "createdat" => asc ? rows.OrderBy(x => x.CreatedAtUtc) : rows.OrderByDescending(x => x.CreatedAtUtc),
            _ => throw new MatchException(400, "Unsupported match sort.")
        };
        if (query.SortBy.Equals("score", StringComparison.OrdinalIgnoreCase))
            ordered = ordered.ThenByDescending(x => x.Listing.Condition == MaterialCondition.NEW || x.Listing.Condition == MaterialCondition.EXCELLENT ? 4
                : x.Listing.Condition == MaterialCondition.GOOD ? 3 : x.Listing.Condition == MaterialCondition.FAIR ? 2
                : x.Listing.Condition == MaterialCondition.POOR ? 1 : 0)
                .ThenBy(x => x.EstimatedTransportCost == null)
                .ThenBy(x => x.Listing.UnitPrice * x.MaterialRequest.RequiredQuantity + x.EstimatedTransportCost)
                .ThenBy(x => x.Distance == null).ThenBy(x => x.Distance).ThenBy(x => x.ListingId);
        return ordered.ThenBy(x => x.Id);
    }
}
