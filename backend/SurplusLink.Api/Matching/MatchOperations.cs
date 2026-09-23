using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Routing;

namespace SurplusLink.Api.Matching;

public sealed partial class MatchService
{
    public async Task<MatchResponse> GetAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var match = await db.Matches.AsNoTracking().Include(x => x.Listing).ThenInclude(x => x.Category)
            .Include(x => x.MaterialRequest).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new MatchException(404, "Match not found.");
        await AuthorizeRequirement(match.MaterialRequestId, actor, manager, ct);
        return Response(match);
    }

    // These operations prepare candidates only. Starting the four-agent workflow
    // and the manager approval gate remain the only path to a reservation.
    public async Task<MatchPage> GenerateCandidatesAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOpenRequirement(id, actor, manager, ct);
        var listings = await db.Listings.AsNoTracking().Where(x => x.CategoryId == request.CategoryId)
            .OrderByDescending(x => x.Status == ListingStatus.ACTIVE && x.AvailableUntil >= request.Deadline &&
                x.SellerId != request.BuyerId && x.Quantity - x.ReservedQuantity >= request.RequiredQuantity &&
                x.Unit.ToLower() == request.Unit.ToLower() && x.UnitPrice * request.RequiredQuantity <= request.MaximumBudget)
            .ThenBy(x => x.UnitPrice).ThenBy(x => x.Id).Take(100).ToListAsync(ct);
        foreach (var listing in listings)
        {
            await GenerateAsync(id, listing.Id, actor, ct);
        }
        await tx.CommitAsync(ct);
        return await ListAsync(id, actor, manager, new MatchQuery(), ct);
    }

    public async Task<MatchPage> RankCandidatesAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOpenRequirement(id, actor, manager, ct);
        var matches = await db.Matches.Include(x => x.Listing).Where(x => x.MaterialRequestId == id &&
            x.Status != MatchStatus.REJECTED).ToListAsync(ct);
        foreach (var match in matches)
        {
            var reason = EligibilityReason(request, match.Listing);
            if (reason is not null) { match.Status = MatchStatus.REJECTED; match.RejectionReason = reason; Audit(match, actor, "REJECT"); continue; }
            // Both distance and total cost reduce the score; unavailable routing
            // earns no route score and never turns into a zero-distance success.
            var cost = match.Listing.UnitPrice * request.RequiredQuantity + (match.EstimatedTransportCost ?? 0);
            match.Score = Math.Round(Math.Clamp(.5m + .3m * Math.Max(0, 1 - cost / request.MaximumBudget) +
                (match.Distance is decimal distance ? .2m / (1 + distance / 100) : 0), 0, 1), 4);
            if (match.Status == MatchStatus.GENERATED) match.Status = MatchStatus.RANKED;
            Audit(match, actor, "RANK");
        }
        await Save(ct);
        await tx.CommitAsync(ct);
        return await ListAsync(id, actor, manager, new MatchQuery(), ct);
    }

    public async Task<MatchResponse> RouteCandidateAsync(Guid id, Guid actor, bool manager,
        ITransportEstimateService transport, CancellationToken ct)
    {
        var requestId = await db.Matches.Where(x => x.Id == id).Select(x => (Guid?)x.MaterialRequestId).SingleOrDefaultAsync(ct)
            ?? throw new MatchException(404, "Match not found.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOpenRequirement(requestId, actor, manager, ct);
        var match = await MutableMatch(id, ct);
        var listing = await db.Listings.AsNoTracking().SingleAsync(x => x.Id == match.ListingId, ct);
        var reason = EligibilityReason(request, listing);
        if (reason is not null)
        {
            match.Status = MatchStatus.REJECTED; match.RejectionReason = reason; Audit(match, actor, "REJECT");
        }
        else
        {
            TransportEstimate? estimate = null;
            if (listing.Latitude is decimal lat && listing.Longitude is decimal lon &&
                request.Latitude is decimal buyerLat && request.Longitude is decimal buyerLon)
                estimate = await transport.EstimateAsync(new(lat, lon, buyerLat, buyerLon), ct);
            var success = estimate is { Route.Success: true, EstimatedTransportCost: >= 0, ErrorCode: null };
            match.Distance = success ? estimate!.Route.DistanceKm : null;
            match.DurationMinutes = success ? estimate!.Route.DurationMinutes : null;
            match.EstimatedTransportCost = success ? estimate!.EstimatedTransportCost : null;
            match.Status = success ? MatchStatus.ROUTED : MatchStatus.ROUTE_FAILED;
            Audit(match, actor, success ? "ROUTE_SUCCEEDED" : "ROUTE_FAILED");
            if (success && (listing.UnitPrice * request.RequiredQuantity + match.EstimatedTransportCost > request.MaximumBudget ||
                match.DurationMinutes > (decimal)(request.Deadline - DateTime.UtcNow).TotalMinutes))
            {
                match.Status = MatchStatus.REJECTED;
                match.RejectionReason = listing.UnitPrice * request.RequiredQuantity + match.EstimatedTransportCost > request.MaximumBudget
                    ? "TOTAL_COST_EXCEEDS_BUDGET" : "DELIVERY_DEADLINE_EXCEEDED";
                Audit(match, actor, "REJECT");
            }
        }
        await Save(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(id, actor, manager, ct);
    }

    private async Task<BuyerRequest> LockOpenRequirement(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await AuthorizeRequirement(id, actor, manager, ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"MaterialRequests\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var request = await db.BuyerRequests.SingleAsync(x => x.Id == id, ct);
        await db.Entry(request).ReloadAsync(ct);
        if (request.Status != BuyerRequestStatus.OPEN || request.Deadline <= DateTime.UtcNow ||
            await db.AgentWorkflows.AnyAsync(x => x.MaterialRequestId == id &&
                (x.Status == AgentWorkflowStatus.RUNNING || x.Status == AgentWorkflowStatus.PENDING_APPROVAL ||
                 x.Status == AgentWorkflowStatus.APPROVED || x.Status == AgentWorkflowStatus.COMPLETED), ct))
            throw new MatchException(409, "Candidate preparation requires an OPEN requirement with no active workflow.");
        return request;
    }

    internal static string? EligibilityReason(BuyerRequest request, Listing listing) =>
        MarketplaceMatchPolicy.RejectionReason(request.BuyerId, listing.SellerId)
        ?? (listing.Status != ListingStatus.ACTIVE ? "LISTING_NOT_ACTIVE" : null)
        ?? (listing.AvailableUntil <= DateTime.UtcNow ? "LISTING_EXPIRED" : null)
        ?? (listing.CategoryId != request.CategoryId ? "CATEGORY_MISMATCH" : null)
        ?? (!string.Equals(listing.Unit, request.Unit, StringComparison.OrdinalIgnoreCase) ? "UNIT_MISMATCH" : null)
        ?? (listing.Quantity - listing.ReservedQuantity < request.RequiredQuantity ? "INSUFFICIENT_QUANTITY" : null)
        ?? (listing.UnitPrice * request.RequiredQuantity > request.MaximumBudget ? "BUDGET_EXCEEDED" : null);

    private static MatchResponse Response(MaterialMatch x) => new(x.Id, x.MaterialRequestId, x.ListingId,
        x.Score, x.Distance, x.EstimatedTransportCost, x.Status.ToString(), x.Status != MatchStatus.REJECTED,
        x.Status == MatchStatus.REJECTED, x.RejectionReason, x.CreatedAtUtc, x.DurationMinutes,
        x.Listing.Title, x.Listing.Category.Name, x.Listing.SellerId, x.MaterialRequest.RequiredQuantity, x.Listing.Unit, x.Listing.UnitPrice,
        x.Listing.AvailableUntil, x.MaterialRequest.Deadline, x.Listing.Quantity - x.Listing.ReservedQuantity,
        x.MaterialRequest.MaximumBudget, x.MaterialRequest.Status.ToString(),
        x.Listing.Seller.FullName, x.Listing.Seller.BusinessName, x.Listing.Condition.ToString(),
        x.Listing.Latitude, x.Listing.Longitude, x.Listing.Seller.Address, false);
}
