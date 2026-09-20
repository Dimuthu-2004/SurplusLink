using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Workflows;

public sealed class WorkflowDecisionRequest
{
    [StringLength(2000)]
    public string? Note { get; init; }
}

public sealed class WorkflowQuery : IValidatableObject
{
    [StringLength(200)] public string? Search { get; init; }
    public string? Status { get; init; }
    [Required, RegularExpression("(?i)^(startedAt|status|stage)$")] public string SortBy { get; init; } = "startedAt";
    [Required, RegularExpression("(?i)^(asc|desc)$")] public string SortDir { get; init; } = "desc";
    [Range(1, 1_000_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.GetNames<AgentWorkflowStatus>().Contains(Status, StringComparer.OrdinalIgnoreCase))
            yield return new("Status must be a named workflow status.", [nameof(Status)]);
    }
}

public sealed record AgentToolCallResponse(
    Guid Id, string ToolName, string InputJson, string OutputJson, string? ErrorJson,
    int RetryCount, DateTime StartedAtUtc, DateTime? CompletedAtUtc, long? DurationMilliseconds);

public sealed record AgentStepResponse(
    Guid Id, int Sequence, string Stage, string Status, string InputJson, string OutputJson,
    string ValidationJson, string? ErrorJson, int RetryCount, DateTime StartedAtUtc,
    DateTime? CompletedAtUtc, long? DurationMilliseconds, IReadOnlyList<AgentToolCallResponse> ToolCalls);

public sealed record ApprovalResponse(
    Guid Id, Guid DecidedByUserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ApprovalDecision Decision,
    string Note, DateTime DecidedAtUtc);

public sealed record AgentWorkflowResponse(
    Guid Id, Guid? MaterialRequestId, Guid? MaterialMatchId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AgentWorkflowStatus Status,
    string CurrentStage, string InputJson, string OutputJson, string ValidationJson, string? ErrorJson,
    string? Decision, int RetryCount, DateTime StartedAtUtc, DateTime? CompletedAtUtc,
    IReadOnlyList<AgentStepResponse> Steps, IReadOnlyList<ApprovalResponse> Approvals);

public sealed record AgentWorkflowListItem(Guid Id, Guid? MaterialRequestId, Guid? MaterialMatchId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AgentWorkflowStatus Status, string CurrentStage,
    int RetryCount, DateTime StartedAtUtc, DateTime? CompletedAtUtc, string? ErrorJson);
public sealed record AgentWorkflowStatusCount(string Status, int Count);
public sealed record AgentWorkflowSummary(
    Guid Id, AgentWorkflowStatus Status, string CurrentStage, int StepCount, int ToolCallCount,
    int RetryCount, int ApprovalCount, DateTime StartedAtUtc, DateTime? CompletedAtUtc,
    IReadOnlyList<AgentWorkflowStatusCount> DecisionCounts);

public sealed class AgentWorkflowException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
