using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Workflows;

public sealed class PersistentRequirementWorkflowStarter(SurplusLinkDbContext db) : IRequirementWorkflowStarter
{
    public async Task<Guid> StartAsync(Guid requirementId, Guid buyerId, CancellationToken cancellationToken)
    {
        var request = await db.BuyerRequests.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == requirementId && x.BuyerId == buyerId, cancellationToken)
            ?? throw new RequirementWorkflowUnavailableException();
        var existing = await db.AgentWorkflows.FirstOrDefaultAsync(
            x => x.MaterialRequestId == requirementId &&
                 (x.Status == AgentWorkflowStatus.RUNNING || x.Status == AgentWorkflowStatus.PENDING_APPROVAL ||
                  x.Status == AgentWorkflowStatus.APPROVED || x.Status == AgentWorkflowStatus.COMPLETED),
            cancellationToken);
        if (existing is not null) return existing.Id;

        var now = DateTime.UtcNow;
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, Status = AgentWorkflowStatus.RUNNING,
            CurrentStage = "QUEUED", StartedAtUtc = now,
            InputJson = JsonSerializer.Serialize(new
            {
                requirementId = request.Id,
                buyerId = request.BuyerId,
                // Title is bounded by the requirement contract and retained only as
                // untrusted planner context; eligibility still comes from fields.
                objective = request.Title.Length <= 2000 ? request.Title : request.Title[..2000]
            }),
            OutputJson = "{}", ValidationJson = "{}"
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        return workflow.Id;
    }
}
