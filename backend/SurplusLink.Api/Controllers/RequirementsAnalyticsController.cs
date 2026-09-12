using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Features.Analytics;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.MANAGER))]
[Route("api/requirements/analytics")]
public sealed class RequirementsAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<RequirementAnalyticsSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RequirementAnalyticsSummary>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetRequirementSummaryAsync(cancellationToken));
}
