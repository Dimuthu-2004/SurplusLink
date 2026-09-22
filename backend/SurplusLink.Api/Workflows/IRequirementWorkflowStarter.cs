namespace SurplusLink.Api.Workflows;

/// <summary>
/// M4-2 integration seam: persist/reuse the canonical AgentWorkflow for this requirement,
/// using the same scoped DbContext and current transaction. Return its real, stable ID.
/// The requirement ID is the idempotency key. Starting a workflow must not reserve material.
/// </summary>
public interface IRequirementWorkflowStarter
{
    Task<Guid> StartAsync(Guid requirementId, Guid buyerId, CancellationToken cancellationToken);
}

/// <summary>No canonical AgentWorkflow persistence exists yet. Replaced by M4-2.</summary>
public sealed class DeferredRequirementWorkflowStarter : IRequirementWorkflowStarter
{
    public Task<Guid> StartAsync(Guid requirementId, Guid buyerId, CancellationToken cancellationToken) =>
        throw new RequirementWorkflowUnavailableException();
}

public sealed class RequirementWorkflowUnavailableException : Exception
{
    public RequirementWorkflowUnavailableException()
        : base("Matching is currently unavailable. Please retry later. Your requirement remains open.") { }
}
