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

    public async Task<RequirementPage> ListAsync(Guid? buyerId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.BuyerRequests.AsNoTracking();
        if (buyerId.HasValue) query = query.Where(x => x.BuyerId == buyerId.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items.Select(RequirementResponse.From).ToArray(), total, page, pageSize);
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
