using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Features.Analytics;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.MANAGER))]
[Route("api/materials/analytics")]
public sealed class MaterialsAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<ListingAnalyticsSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListingAnalyticsSummary>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetListingSummaryAsync(cancellationToken));
}
