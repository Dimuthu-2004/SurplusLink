using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Api.Requirements;

public sealed class RequirementService(SurplusLinkDbContext db, IRequirementWorkflowStarter workflowStarter)
{
    public async Task<RequirementResponse> CreateAsync(Guid buyerId, SaveRequirementRequest input, CancellationToken ct)
    {
        var category = await ValidateAsync(input, ct);
        var request = new BuyerRequest { Id = Guid.NewGuid(), BuyerId = buyerId, Title = category.Name + " requirement" };
        Apply(request, input);
        db.BuyerRequests.Add(request);
        Audit(request, "CREATED");
        await db.SaveChangesAsync(ct);
        return RequirementResponse.From(request);
    }

    public async Task<RequirementResponse> GetAsync(Guid id, Guid actorId, bool manager, CancellationToken ct)
    {
        var request = await db.BuyerRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new RequirementException(404, "Requirement was not found.");
        if (!manager) Own(request, actorId);
        return RequirementResponse.From(request);
    }

    public async Task<RequirementPage> ListAsync(Guid? buyerId, RequirementQuery input, CancellationToken ct)
    {
        var query = db.BuyerRequests.AsNoTracking();
        if (buyerId.HasValue) query = query.Where(x => x.BuyerId == buyerId.Value);
        query = RequirementQueryBuilder.Filter(query, input);
        var total = await query.CountAsync(ct);
        var items = await RequirementQueryBuilder.Sort(query, input)
            .Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToListAsync(ct);
        return new(items.Select(RequirementResponse.From).ToArray(), total, input.Page, input.PageSize);
    }

    public async Task<RequirementHistoryPage> HistoryAsync(Guid id, Guid actorId, bool manager, RequirementPageQuery input, CancellationToken ct)
    {
        // Authorize before reading audits, including when the audit collection is empty.
        await GetAsync(id, actorId, manager, ct);
        var query = db.AuditLogs.AsNoTracking().Where(x => x.EntityId == id &&
            (x.EntityType == nameof(BuyerRequest) || x.EntityType == "MaterialRequest"));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToListAsync(ct);
        return new(items.Select(RequirementHistoryEntry.From).ToArray(), total, input.Page, input.PageSize);
    }

    public async Task<RequirementAnalyticsSummary> SummaryAsync(int upcomingDays, CancellationToken ct)
    {
        // All aggregates and the preview describe one consistent database snapshot.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var now = DateTime.UtcNow;
        var until = now.AddDays(upcomingDays);
        var query = db.BuyerRequests.AsNoTracking();
        var statusCounts = await query.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        var statuses = Enum.GetValues<BuyerRequestStatus>()
            .Select(status => new RequirementStatusCount(status.ToString(), statusCounts.GetValueOrDefault(status))).ToArray();
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(category => new RequirementCategoryCount(category.Id, category.Name,
                db.BuyerRequests.Count(x => x.CategoryId == category.Id))).ToListAsync(ct);
        var upcoming = query.Where(x => x.Deadline >= now && x.Deadline <= until &&
            (x.Status == BuyerRequestStatus.OPEN || x.Status == BuyerRequestStatus.MATCHING ||
             x.Status == BuyerRequestStatus.MATCH_FOUND || x.Status == BuyerRequestStatus.PENDING_APPROVAL ||
             x.Status == BuyerRequestStatus.APPROVED));
        var upcomingCount = await upcoming.CountAsync(ct);
        var deadlines = await upcoming.OrderBy(x => x.Deadline).ThenBy(x => x.Id).Take(10).ToListAsync(ct);
        var averageBudget = await query.AverageAsync(x => (decimal?)x.MaximumBudget, ct);
        // Quantities in different units must not be averaged together.
        var quantityAverages = await query.GroupBy(x => x.Unit).OrderBy(g => g.Key)
            .Select(g => new RequirementQuantityAverage(g.Key, g.Average(x => x.RequiredQuantity), g.Count())).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(statuses.Sum(x => x.Count), statuses, categories,
            statusCounts.GetValueOrDefault(BuyerRequestStatus.OPEN), now, until, upcomingCount,
            deadlines.Select(RequirementResponse.From).ToArray(), averageBudget, quantityAverages);
    }

    public async Task<RequirementResponse> UpdateAsync(Guid id, Guid buyerId, SaveRequirementRequest input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOwnedAsync(id, buyerId, ct);
        RequireState(request, BuyerRequestStatus.DRAFT);
        await ValidateAsync(input, ct);
        Apply(request, input);
        Audit(request, "UPDATED");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RequirementResponse.From(request);
    }

    public async Task DeleteAsync(Guid id, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOwnedAsync(id, buyerId, ct);
        RequireState(request, BuyerRequestStatus.DRAFT);
        db.BuyerRequests.Remove(request);
        Audit(request, "DELETED");
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new RequirementException(409, "This requirement has linked records and cannot be deleted.");
        }
    }

    public async Task<RequirementResponse> SubmitAsync(Guid id, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOwnedAsync(id, buyerId, ct);
        RequireState(request, BuyerRequestStatus.DRAFT);
        FutureDeadline(request.Deadline);
        request.Status = BuyerRequestStatus.OPEN;
        Audit(request, "SUBMITTED");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RequirementResponse.From(request);
    }

    public async Task<StartMatchingResponse> StartMatchingAsync(Guid id, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOwnedAsync(id, buyerId, ct);
        RequireState(request, BuyerRequestStatus.OPEN);
        FutureDeadline(request.Deadline);
        // No reservation dependency: M4-2 must create/reuse the real workflow inside this transaction.
        var workflowId = await workflowStarter.StartAsync(id, buyerId, ct);
        if (workflowId == Guid.Empty) throw new RequirementWorkflowUnavailableException();
        request.Status = BuyerRequestStatus.MATCHING;
        Audit(request, "MATCHING_STARTED");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(RequirementResponse.From(request), workflowId);
    }

    public async Task<RequirementResponse> CancelAsync(Guid id, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await LockOwnedAsync(id, buyerId, ct);
        if (request.Status is not BuyerRequestStatus.DRAFT and not BuyerRequestStatus.OPEN)
            throw new RequirementException(409, "Only draft or open requirements can be cancelled here.");
        request.Status = BuyerRequestStatus.CANCELLED;
        Audit(request, "CANCELLED");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RequirementResponse.From(request);
    }

    private async Task<BuyerRequest> LockOwnedAsync(Guid id, Guid buyerId, CancellationToken ct)
    {
        // Serialize lifecycle changes, including concurrent starts, across API instances.
        var request = await db.BuyerRequests.FromSqlInterpolated(
            $"""SELECT *, xmin FROM "MaterialRequests" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(ct) ?? throw new RequirementException(404, "Requirement was not found.");
        Own(request, buyerId);
        return request;
    }

    private async Task<Category> ValidateAsync(SaveRequirementRequest input, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new RequirementException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
        FutureDeadline(input.Deadline!.Value.UtcDateTime);
        return await db.Categories.SingleOrDefaultAsync(x => x.Id == input.CategoryId, ct)
            ?? throw new RequirementException(400, "Category does not exist.");
    }

    private static void FutureDeadline(DateTime deadline)
    {
        if (deadline <= DateTime.UtcNow)
            throw new RequirementException(400, "Deadline must be in the future.");
    }

    private static void Own(BuyerRequest request, Guid buyerId)
    {
        if (request.BuyerId != buyerId)
            throw new RequirementException(403, "You can only access your own requirements.");
    }

    private static void RequireState(BuyerRequest request, BuyerRequestStatus state)
    {
        if (request.Status != state)
            throw new RequirementException(409, $"This operation requires status {state}; current status is {request.Status}.");
    }

    private static void Apply(BuyerRequest request, SaveRequirementRequest input)
    {
        request.CategoryId = input.CategoryId;
        request.Notes = input.Notes?.Trim() ?? string.Empty;
        request.RequiredQuantity = input.RequiredQuantity;
        request.Unit = input.Unit.Trim();
        request.MaximumBudget = input.MaximumBudget;
        request.Deadline = input.Deadline!.Value.UtcDateTime;
        request.Latitude = input.Latitude;
        request.Longitude = input.Longitude;
    }

    private void Audit(BuyerRequest request, string action) => db.AuditLogs.Add(new AuditLog
    {
        Id = Guid.NewGuid(), ActorUserId = request.BuyerId, EntityType = nameof(BuyerRequest),
        EntityId = request.Id, Action = action
    });
}
