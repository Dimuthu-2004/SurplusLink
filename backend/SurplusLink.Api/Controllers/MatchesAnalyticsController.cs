using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Features.Analytics;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.MANAGER))]
[Route("api/matches/analytics")]
public sealed class MatchesAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<MatchAnalyticsSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchAnalyticsSummary>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetMatchSummaryAsync(cancellationToken));
}
