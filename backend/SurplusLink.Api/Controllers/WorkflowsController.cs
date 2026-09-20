using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize(Roles = "MANAGER")]
public sealed class WorkflowsController(AgentWorkflowService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentWorkflowResponse>> Get(Guid id, CancellationToken ct) =>
        await Execute(() => service.GetAsync(id, ct));

    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(AgentWorkflowSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentWorkflowSummary>> Summary(Guid id, CancellationToken ct) =>
        await Execute(() => service.SummaryAsync(id, ct));

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentWorkflowResponse>> Approve(Guid id, WorkflowDecisionRequest request, CancellationToken ct) =>
        await Execute(() => service.ApproveAsync(id, Actor(), request.Note, ct));

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentWorkflowResponse>> Reject(Guid id, WorkflowDecisionRequest request, CancellationToken ct) =>
        await Execute(() => service.RejectAsync(id, Actor(), request.Note, ct));

    [HttpPost("{id:guid}/revise")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentWorkflowResponse>> Revise(Guid id, WorkflowDecisionRequest request, CancellationToken ct) =>
        await Execute(() => service.ReviseAsync(id, Actor(), request.Note, ct));

    private Guid Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw new AgentWorkflowException(401, "A valid manager identity is required.");

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (AgentWorkflowException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails { Status = exception.StatusCode, Detail = exception.Message });
        }
    }
}
