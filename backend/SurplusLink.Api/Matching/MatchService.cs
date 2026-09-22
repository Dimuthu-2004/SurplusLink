using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Matching;

public sealed partial class MatchService(SurplusLinkDbContext db)
{
    public async Task<MatchPage> ListAsync(Guid requirementId, Guid actor, bool manager, MatchQuery query, CancellationToken ct)
    {
        Validate(query);
        await AuthorizeRequirement(requirementId, actor, manager, ct);
        var rows = MatchQueryBuilder.Filter(db.Matches.AsNoTracking().Where(x => x.MaterialRequestId == requirementId), query);
        var total = await rows.CountAsync(ct);
        var items = await MatchQueryBuilder.Sort(rows, query).Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).Select(x => new MatchResponse(x.Id, x.MaterialRequestId, x.ListingId,
                x.Score, x.Distance, x.EstimatedTransportCost, x.Status.ToString(), x.Status != MatchStatus.REJECTED,
                x.Status == MatchStatus.REJECTED, x.RejectionReason, x.CreatedAtUtc, x.DurationMinutes,
                x.Listing.Title, x.Listing.Category.Name, x.Listing.SellerId,
                x.MaterialRequest.RequiredQuantity, x.Listing.Unit, x.Listing.UnitPrice)).ToListAsync(ct);
        return new(items, total, Pages(total, query.PageSize), query.Page, query.PageSize);
    }

    public async Task<MatchHistoryPage> HistoryAsync(Guid id, Guid actor, bool manager, MatchPageQuery query, CancellationToken ct)
    {
        Validate(query);
        var match = await db.Matches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new MatchException(404, "Match not found.");
        await AuthorizeRequirement(match.MaterialRequestId, actor, manager, ct);
        var logs = db.AuditLogs.AsNoTracking().Where(x => x.EntityType == nameof(MaterialMatch) && x.EntityId == id);
        var total = await logs.CountAsync(ct);
        var page = await logs.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new(page.Select(x => new MatchHistoryEntry(x.Id, x.ActorUserId,
            x.Action.StartsWith("ROUTE_") ? "ROUTE" : x.Action,
            x.Action.StartsWith("ROUTE_") ? x.Action[6..] : null, x.CreatedAtUtc)).ToArray(),
            total, Pages(total, query.PageSize), query.Page, query.PageSize);
    }

    public async Task<MatchAnalyticsSummary> SummaryAsync(CancellationToken ct, Guid? requirementId = null)
    {
        var rows = db.Matches.AsNoTracking();
        if (requirementId.HasValue) rows = rows.Where(x => x.MaterialRequestId == requirementId.Value);
        var total = await rows.CountAsync(ct);
        var score = await rows.Select(x => (decimal?)x.Score).AverageAsync(ct);
        var distance = await rows.Select(x => x.Distance).AverageAsync(ct);
        var reasons = await rows.Where(x => x.Status == MatchStatus.REJECTED && x.RejectionReason != null)
            .GroupBy(x => x.RejectionReason).Select(x => new { Reason = x.Key!, Count = x.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Reason).Take(10).ToListAsync(ct);
        var attempts = db.AuditLogs.AsNoTracking().Where(x => x.EntityType == nameof(MaterialMatch));
        if (requirementId.HasValue) attempts = attempts.Where(x => rows.Any(m => m.Id == x.EntityId));
        var success = await attempts.CountAsync(x => x.Action == "ROUTE_SUCCEEDED", ct);
        var failure = await attempts.CountAsync(x => x.Action == "ROUTE_FAILED", ct);
        var count = success + failure;
        return new(total, score, distance, reasons.Select(x => new RejectionReasonCount(x.Reason, x.Count)).ToArray(), success, failure,
            count == 0 ? null : (double)success / count, count == 0 ? null : (double)failure / count,
            await rows.CountAsync(x => x.Status != MatchStatus.REJECTED, ct),
            await rows.CountAsync(x => x.Status == MatchStatus.REJECTED, ct),
            await rows.AverageAsync(x => x.EstimatedTransportCost, ct));
    }

    // Trusted workflow integration boundary: scores and route results are computed by callers,
    // never supplied by an unauthenticated or buyer-facing write endpoint.
    public async Task<MaterialMatch> GenerateAsync(Guid requirementId, Guid listingId, Guid? actor, CancellationToken ct)
    {
        var request = await db.BuyerRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requirementId, ct)
            ?? throw new MatchException(404, "Requirement not found.");
        var listing = await db.Listings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == listingId, ct)
            ?? throw new MatchException(404, "Listing not found.");
        if (request.Status is not (BuyerRequestStatus.OPEN or BuyerRequestStatus.MATCHING))
            throw new MatchException(409, "Matching requires an OPEN or MATCHING requirement.");
        if (request.Deadline <= DateTime.UtcNow)
            throw new MatchException(409, "The requirement has expired.");
        if (await db.Matches.AnyAsync(x => x.MaterialRequestId == requirementId && x.ListingId == listingId, ct))
            throw new MatchException(409, "This candidate has already been generated.");
        var reason = EligibilityReason(request, listing);
        var match = new MaterialMatch
        {
            Id = Guid.NewGuid(), MaterialRequestId = requirementId, ListingId = listingId,
            Status = reason is null ? MatchStatus.GENERATED : MatchStatus.REJECTED, RejectionReason = reason
        };
        db.Matches.Add(match);
        Audit(match, actor, "GENERATE");
        if (reason is not null) Audit(match, actor, "REJECT");
        await Save(ct);
        return match;
    }

    public async Task RankAsync(Guid id, decimal score, Guid? actor, CancellationToken ct)
    {
        if (score < 0 || score > 1) throw new MatchException(400, "Score must be between zero and one.");
        var match = await MutableMatch(id, ct);
        match.Score = score;
        if (match.Status == MatchStatus.GENERATED) match.Status = MatchStatus.RANKED;
        Audit(match, actor, "RANK");
        await Save(ct);
    }

    public async Task RecordRouteAsync(Guid id, bool succeeded, decimal? distance, decimal? estimatedTransportCost,
        Guid? actor, CancellationToken ct)
    {
        if (succeeded && (distance is null || estimatedTransportCost is null || distance < 0 || estimatedTransportCost < 0))
            throw new MatchException(400, "A successful route requires nonnegative distance and transport cost.");
        if (!succeeded && (distance is not null || estimatedTransportCost is not null))
            throw new MatchException(400, "A failed route must not contain distance or transport cost.");
        var match = await MutableMatch(id, ct);
        match.Distance = distance;
        match.EstimatedTransportCost = estimatedTransportCost;
        match.Status = succeeded ? MatchStatus.ROUTED : MatchStatus.ROUTE_FAILED;
        Audit(match, actor, succeeded ? "ROUTE_SUCCEEDED" : "ROUTE_FAILED");
        await Save(ct);
    }

    public async Task RejectAsync(Guid id, string reason, Guid? actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 200)
            throw new MatchException(400, "A rejection reason of at most 200 characters is required.");
        var match = await MutableMatch(id, ct);
        match.Status = MatchStatus.REJECTED;
        match.RejectionReason = reason.Trim();
        Audit(match, actor, "REJECT");
        await Save(ct);
    }

    private async Task<MaterialMatch> MutableMatch(Guid id, CancellationToken ct)
    {
        var match = await db.Matches.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new MatchException(404, "Match not found.");
        if (match.Status == MatchStatus.REJECTED) throw new MatchException(409, "A rejected match cannot be changed.");
        // Even repeated identical route/rank outcomes must check xmin before appending an audit event.
        db.Entry(match).Property(x => x.Status).IsModified = true;
        return match;
    }

    private async Task AuthorizeRequirement(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var owner = await db.BuyerRequests.Where(x => x.Id == id).Select(x => (Guid?)x.BuyerId).SingleOrDefaultAsync(ct)
            ?? throw new MatchException(404, "Requirement not found.");
        if (!manager && owner != actor) throw new MatchException(403, "This requirement belongs to another buyer.");
    }

    private void Audit(MaterialMatch match, Guid? actor, string action) => db.AuditLogs.Add(new AuditLog
    {
        Id = Guid.NewGuid(), EntityType = nameof(MaterialMatch), EntityId = match.Id, ActorUserId = actor, Action = action
    });

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new MatchException(409, "The match changed. Reload and retry."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new MatchException(409, "This candidate has already been generated."); }
    }

    private static int Pages(int total, int size) => (int)Math.Ceiling((double)total / size);
    private static void Validate(object query)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
            throw new MatchException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
    }
}
