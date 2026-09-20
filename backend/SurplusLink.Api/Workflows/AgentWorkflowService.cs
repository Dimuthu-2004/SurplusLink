using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Workflows;

public sealed class AgentWorkflowService(SurplusLinkDbContext db)
{
    public async Task<AgentWorkflowResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var workflow = await LoadAsync(id, ct) ?? throw NotFound();
        return ToResponse(workflow);
    }

    public async Task<AgentWorkflowSummary> SummaryAsync(Guid id, CancellationToken ct)
    {
        var workflow = await db.AgentWorkflows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound();
        var statuses = await db.Approvals.AsNoTracking().Where(x => x.AgentWorkflowId == id)
            .GroupBy(x => x.Decision).Select(x => new AgentWorkflowStatusCount(x.Key.ToString(), x.Count()))
            .ToListAsync(ct);
        return new(workflow.Id, workflow.Status, workflow.CurrentStage,
            await db.AgentSteps.CountAsync(x => x.AgentWorkflowId == id, ct),
            await db.AgentToolCalls.CountAsync(x => x.AgentStep.AgentWorkflowId == id, ct),
            workflow.RetryCount, await db.Approvals.CountAsync(x => x.AgentWorkflowId == id, ct),
            workflow.StartedAtUtc, workflow.CompletedAtUtc, statuses);
    }

    public Task<AgentWorkflowResponse> ApproveAsync(Guid id, Guid managerId, string? note, CancellationToken ct) =>
        DecideAsync(id, managerId, ApprovalDecision.APPROVED, note, ct);

    public Task<AgentWorkflowResponse> RejectAsync(Guid id, Guid managerId, string? note, CancellationToken ct) =>
        DecideAsync(id, managerId, ApprovalDecision.REJECTED, note, ct);

    public async Task<AgentWorkflowResponse> ReviseAsync(Guid id, Guid managerId, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var workflow = await LockAsync(id, ct) ?? throw NotFound();
        EnsureDecisionState(workflow);
        var cleanNote = NormalizeNote(note);
        workflow.Status = AgentWorkflowStatus.REVISION_REQUESTED;
        workflow.CurrentStage = "REVISION";
        workflow.Decision = cleanNote;
        db.AgentSteps.Add(new AgentStep
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, Sequence = await db.AgentSteps.CountAsync(x => x.AgentWorkflowId == workflow.Id, ct) + 1,
            Stage = "REVISION", Status = "PENDING",
            InputJson = "{}", OutputJson = "{}", ValidationJson = "{}", StartedAtUtc = DateTime.UtcNow
        });
        db.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, DecidedByUserId = managerId, Decision = ApprovalDecision.REVISION_REQUESTED,
            Note = cleanNote, DecidedAtUtc = DateTime.UtcNow
        });
        Audit(workflow.Id, managerId, "WORKFLOW_REVISION_REQUESTED");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(await LoadAsync(id, ct) ?? workflow);
    }

    private async Task<AgentWorkflowResponse> DecideAsync(
        Guid id, Guid managerId, ApprovalDecision decision, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var workflow = await LockAsync(id, ct) ?? throw NotFound();
        if (workflow.Status == AgentWorkflowStatus.APPROVED && decision == ApprovalDecision.APPROVED)
        {
            await tx.CommitAsync(ct);
            return ToResponse(await LoadAsync(id, ct) ?? workflow);
        }
        EnsureDecisionState(workflow);
        var cleanNote = NormalizeNote(note);

        if (decision == ApprovalDecision.APPROVED)
        {
            if (workflow.MaterialMatchId is not Guid matchId)
                throw new AgentWorkflowException(409, "Approval requires a workflow match.");
            var match = await db.Matches.Include(x => x.Listing).Include(x => x.MaterialRequest)
                .SingleOrDefaultAsync(x => x.Id == matchId, ct)
                ?? throw new AgentWorkflowException(409, "The workflow match was not found.");
            if (MarketplaceMatchPolicy.RejectionReason(match.MaterialRequest.BuyerId, match.Listing.SellerId) is not null)
                throw new AgentWorkflowException(409, "A workflow cannot approve a self-dealing match.");
            if (match.Listing.Status is not ListingStatus.ACTIVE and not ListingStatus.AVAILABLE and not ListingStatus.RESERVED)
                throw new AgentWorkflowException(409, "The listing is not available for reservation.");
            if (match.MaterialRequest.Status is not BuyerRequestStatus.OPEN and not BuyerRequestStatus.MATCH_FOUND)
                throw new AgentWorkflowException(409, "The material request is not open for reservation.");
            if (match.Listing.CategoryId != match.MaterialRequest.CategoryId)
                throw new AgentWorkflowException(409, "The workflow match categories do not match.");

            var existing = await db.Reservations.AnyAsync(x => x.ListingId == match.ListingId &&
                x.MaterialRequestId == match.MaterialRequestId && x.Status != ReservationStatus.RELEASED &&
                x.Status != ReservationStatus.CANCELLED, ct);
            if (!existing)
            {
                var quantity = match.MaterialRequest.RequiredQuantity;
                if (match.Listing.ReservedQuantity + quantity > match.Listing.Quantity)
                    throw new AgentWorkflowException(409, "Insufficient available quantity.");
                match.Listing.ReservedQuantity += quantity;
                if (match.Listing.ReservedQuantity == match.Listing.Quantity)
                    match.Listing.Status = ListingStatus.RESERVED;
                db.Reservations.Add(new Reservation
                {
                    Id = Guid.NewGuid(), ListingId = match.ListingId, MaterialRequestId = match.MaterialRequestId,
                    Quantity = quantity, Status = ReservationStatus.ACTIVE
                });
            }
            workflow.Status = AgentWorkflowStatus.APPROVED;
            workflow.CurrentStage = "APPROVED";
            workflow.CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            workflow.Status = AgentWorkflowStatus.REJECTED;
            workflow.CurrentStage = "REJECTED";
            workflow.CompletedAtUtc = DateTime.UtcNow;
        }

        workflow.Decision = cleanNote;
        db.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, DecidedByUserId = managerId, Decision = decision,
            Note = cleanNote, DecidedAtUtc = DateTime.UtcNow
        });
        Audit(workflow.Id, managerId, $"WORKFLOW_{decision}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(await LoadAsync(id, ct) ?? workflow);
    }

    private async Task<AgentWorkflow?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.AgentWorkflows.AsNoTracking().Include(x => x.Steps).ThenInclude(x => x.ToolCalls)
            .Include(x => x.Approvals).SingleOrDefaultAsync(x => x.Id == id, ct);

    private async Task<AgentWorkflow?> LockAsync(Guid id, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"AgentWorkflows\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        return await db.AgentWorkflows.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    private static void EnsureDecisionState(AgentWorkflow workflow)
    {
        if (workflow.Status is not AgentWorkflowStatus.PENDING_APPROVAL and not AgentWorkflowStatus.REVISION_REQUESTED)
            throw new AgentWorkflowException(409, $"This decision requires PENDING_APPROVAL or REVISION_REQUESTED; current status is {workflow.Status}.");
    }

    private static string NormalizeNote(string? note) => note?.Trim() ?? string.Empty;

    private void Audit(Guid workflowId, Guid managerId, string action) => db.AuditLogs.Add(new AuditLog
    {
        Id = Guid.NewGuid(), ActorUserId = managerId, EntityType = nameof(AgentWorkflow), EntityId = workflowId, Action = action
    });

    private static AgentWorkflowException NotFound() => new(404, "Workflow was not found.");

    private static AgentWorkflowResponse ToResponse(AgentWorkflow workflow) => new(
        workflow.Id, workflow.MaterialRequestId, workflow.MaterialMatchId, workflow.Status, workflow.CurrentStage,
        workflow.InputJson, workflow.OutputJson, workflow.ValidationJson, workflow.ErrorJson, workflow.Decision,
        workflow.RetryCount, workflow.StartedAtUtc, workflow.CompletedAtUtc,
        workflow.Steps.OrderBy(x => x.Sequence).Select(ToStep).ToArray(),
        workflow.Approvals.OrderBy(x => x.DecidedAtUtc).Select(ToApproval).ToArray());

    private static AgentStepResponse ToStep(AgentStep step) => new(
        step.Id, step.Sequence, step.Stage, step.Status, step.InputJson, step.OutputJson, step.ValidationJson,
        step.ErrorJson, step.RetryCount, step.StartedAtUtc, step.CompletedAtUtc, step.DurationMilliseconds,
        step.ToolCalls.OrderBy(x => x.StartedAtUtc).Select(ToToolCall).ToArray());

    private static AgentToolCallResponse ToToolCall(AgentToolCall call) => new(
        call.Id, call.ToolName, call.InputJson, call.OutputJson, call.ErrorJson, call.RetryCount,
        call.StartedAtUtc, call.CompletedAtUtc, call.DurationMilliseconds);

    private static ApprovalResponse ToApproval(Approval approval) => new(
        approval.Id, approval.DecidedByUserId, approval.Decision, approval.Note, approval.DecidedAtUtc);
}
