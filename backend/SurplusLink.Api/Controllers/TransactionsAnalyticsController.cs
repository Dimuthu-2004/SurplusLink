using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Features.Analytics;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.MANAGER))]
[Route("api/transactions/analytics")]
public sealed class TransactionsAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<TransactionAnalyticsSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionAnalyticsSummary>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetTransactionSummaryAsync(cancellationToken));
}
