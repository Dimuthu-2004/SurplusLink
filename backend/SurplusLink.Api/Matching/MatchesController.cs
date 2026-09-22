using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Matching;

[ApiController]
[Route("api/matches")]
[Authorize(Roles = "BUYER,MANAGER")]
public sealed class MatchesController(MatchService service) : ControllerBase
{
    [HttpGet("requirement/{id:guid}")]
    [ProducesResponseType(typeof(MatchPage), 200)]
    public Task<IActionResult> List(Guid id, [FromQuery] MatchQuery query, CancellationToken ct) =>
        Handle(async actor => Ok(await service.ListAsync(id, actor, User.IsInRole("MANAGER"), query, ct)));

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(MatchHistoryPage), 200)]
    public Task<IActionResult> History(Guid id, [FromQuery] MatchPageQuery query, CancellationToken ct) =>
        Handle(async actor => Ok(await service.HistoryAsync(id, actor, User.IsInRole("MANAGER"), query, ct)));

    [HttpGet("analytics/summary")]
    [Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(MatchAnalyticsSummary), 200)]
    public Task<IActionResult> Summary(CancellationToken ct, [FromQuery] Guid? requirementId = null) =>
        Handle(async _ => Ok(await service.SummaryAsync(ct, requirementId)));

    private async Task<IActionResult> Handle(Func<Guid, Task<IActionResult>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor))
            return Unauthorized();
        try { return await action(actor); }
        catch (MatchException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }
}
