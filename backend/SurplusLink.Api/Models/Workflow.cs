namespace SurplusLink.Api.Models;

public sealed class Workflow : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid MaterialMatchId { get; set; }

    public WorkflowStatus Status { get; set; }

    public MaterialMatch MaterialMatch { get; set; } = null!;
}
