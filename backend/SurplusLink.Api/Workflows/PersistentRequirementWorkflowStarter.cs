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
                 x.Status != AgentWorkflowStatus.REJECTED && x.Status != AgentWorkflowStatus.FAILED,
            cancellationToken);
        if (existing is not null) return existing.Id;

        var now = DateTime.UtcNow;
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, Status = AgentWorkflowStatus.RUNNING,
            CurrentStage = "MATCHING", StartedAtUtc = now,
            InputJson = JsonSerializer.Serialize(new { requirementId = request.Id, buyerId = request.BuyerId }),
            OutputJson = "{}", ValidationJson = "{}"
        };
        workflow.Steps.Add(new AgentStep
        {
            Id = Guid.NewGuid(), Sequence = 1, Stage = "MATCHING", Status = "RUNNING",
            InputJson = workflow.InputJson, OutputJson = "{}", ValidationJson = "{}", StartedAtUtc = now
        });
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        return workflow.Id;
    }
}
