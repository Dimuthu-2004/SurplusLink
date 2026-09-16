using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Api.Requirements;

[ApiController]
[Route("api/requirements")]
[Authorize(Roles = "BUYER,MANAGER")]
public sealed class RequirementsController(RequirementService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(RequirementResponse), 201)]
    public Task<IActionResult> Create(SaveRequirementRequest input, CancellationToken ct) => Handle(async actor =>
    {
        var result = await service.CreateAsync(actor, input, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    });

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequirementResponse), 200)]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Handle(async actor => Ok(await service.GetAsync(id, actor, User.IsInRole("MANAGER"), ct)));

    [HttpGet("my")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(RequirementPage), 200)]
    public Task<IActionResult> My(CancellationToken ct, [Range(1, 1000000)] int page = 1, [Range(1, 100)] int pageSize = 20) =>
        Handle(async actor => Ok(await service.ListAsync(actor, page, pageSize, ct)));

    [HttpGet]
    [Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(RequirementPage), 200)]
    public Task<IActionResult> Monitor(CancellationToken ct, [Range(1, 1000000)] int page = 1, [Range(1, 100)] int pageSize = 20) =>
        Handle(async _ => Ok(await service.ListAsync(null, page, pageSize, ct)));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(RequirementResponse), 200)]
    public Task<IActionResult> Update(Guid id, SaveRequirementRequest input, CancellationToken ct) =>
        Handle(async actor => Ok(await service.UpdateAsync(id, actor, input, ct)));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(204)]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(async actor =>
    {
        await service.DeleteAsync(id, actor, ct);
        return NoContent();
    });

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(RequirementResponse), 200)]
    public Task<IActionResult> Submit(Guid id, CancellationToken ct) =>
        Handle(async actor => Ok(await service.SubmitAsync(id, actor, ct)));

    [HttpPost("{id:guid}/start-matching")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(StartMatchingResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> StartMatching(Guid id, CancellationToken ct) =>
        Handle(async actor => Ok(await service.StartMatchingAsync(id, actor, ct)));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType(typeof(RequirementResponse), 200)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        Handle(async actor => Ok(await service.CancelAsync(id, actor, ct)));

    private async Task<IActionResult> Handle(Func<Guid, Task<IActionResult>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor))
            return Unauthorized();
        try { return await action(actor); }
        catch (RequirementException exception)
        {
            return Problem(statusCode: exception.StatusCode, detail: exception.Message);
        }
        catch (RequirementWorkflowUnavailableException exception)
        {
            return Problem(statusCode: 503, detail: exception.Message);
        }
    }
}
