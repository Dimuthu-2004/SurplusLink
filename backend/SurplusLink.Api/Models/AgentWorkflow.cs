namespace SurplusLink.Api.Models;

public enum AgentWorkflowStatus
{
    RUNNING,
    PENDING_APPROVAL,
    REVISION_REQUESTED,
    APPROVED,
    REJECTED,
    FAILED,
    COMPLETED
}

public enum ApprovalDecision
{
    APPROVED,
    REJECTED,
    REVISION_REQUESTED
}

public sealed class AgentWorkflow : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid? MaterialRequestId { get; set; }
    public Guid? MaterialMatchId { get; set; }
    public AgentWorkflowStatus Status { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public string ValidationJson { get; set; } = "{}";
    public string? ErrorJson { get; set; }
    public string? Decision { get; set; }
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public BuyerRequest? MaterialRequest { get; set; }
    public MaterialMatch? MaterialMatch { get; set; }
    public ICollection<AgentStep> Steps { get; } = new List<AgentStep>();
    public ICollection<Approval> Approvals { get; } = new List<Approval>();
}

public sealed class AgentStep : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid AgentWorkflowId { get; set; }
    public int Sequence { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public string ValidationJson { get; set; } = "{}";
    public string? ErrorJson { get; set; }
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMilliseconds { get; set; }
    public AgentWorkflow AgentWorkflow { get; set; } = null!;
    public ICollection<AgentToolCall> ToolCalls { get; } = new List<AgentToolCall>();
}

public sealed class AgentToolCall : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid AgentStepId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public string? ErrorJson { get; set; }
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMilliseconds { get; set; }
    public AgentStep AgentStep { get; set; } = null!;
}

public sealed class Approval : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid AgentWorkflowId { get; set; }
    public Guid DecidedByUserId { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime DecidedAtUtc { get; set; }
    public AgentWorkflow AgentWorkflow { get; set; } = null!;
    public User DecidedByUser { get; set; } = null!;
}
