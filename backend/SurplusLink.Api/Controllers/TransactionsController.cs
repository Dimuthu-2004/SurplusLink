using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;
using SurplusLink.Api.Transactions;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "MANAGER")]
public sealed class TransactionsController(TransactionService service) : ControllerBase
{
    [HttpGet("offers")]
    public Task<ActionResult<object>> Offers([FromQuery] OfferQuery query, CancellationToken ct) =>
        Execute(async () =>
        {
            var result = await service.ListOffersAsync(query, ct);
            return (object)new { items = result.Items, total = result.Total, page = query.Page, pageSize = query.PageSize,
                totalPages = (int)Math.Ceiling(result.Total / (double)query.PageSize) };
        });

    [HttpGet("transactions")]
    public Task<ActionResult<object>> Transactions([FromQuery] TransactionQuery query, CancellationToken ct) =>
        Execute(async () =>
        {
            var result = await service.ListTransactionsAsync(query, ct);
            return (object)new { items = result.Items, total = result.Total, page = query.Page, pageSize = query.PageSize,
                totalPages = (int)Math.Ceiling(result.Total / (double)query.PageSize) };
        });

    [HttpGet("transactions/{id:guid}/history")]
    public Task<ActionResult<TransactionHistoryPage>> History(Guid id, [FromQuery] TransactionQuery query, CancellationToken ct) =>
        Execute(() => service.HistoryAsync(id, query, ct));

    [HttpGet("transactions/analytics/summary")]
    public Task<ActionResult<TransactionAnalyticsSummary>> Analytics(CancellationToken ct) =>
        Execute(() => service.AnalyticsAsync(ct));

    [HttpPost("offers/{id:guid}/approve")]
    public Task<IActionResult> ApproveOffer(Guid id, CancellationToken ct) => DecideOffer(id, OfferStatus.ACCEPTED, ct);

    [HttpPost("offers/{id:guid}/reject")]
    public Task<IActionResult> RejectOffer(Guid id, CancellationToken ct) => DecideOffer(id, OfferStatus.REJECTED, ct);

    [HttpPost("offers/{id:guid}/revise")]
    public Task<IActionResult> ReviseOffer(Guid id, CancellationToken ct) => DecideOffer(id, OfferStatus.REVISION_REQUESTED, ct);

    [HttpPost("transactions/{id:guid}/approve")]
    public Task<ActionResult<TransactionResponse>> Approve(Guid id, CancellationToken ct) =>
        Execute(() => service.ApproveAsync(id, Actor(), ct));

    [HttpPost("transactions/{id:guid}/complete")]
    public Task<ActionResult<TransactionResponse>> Complete(Guid id, CancellationToken ct) =>
        Execute(() => service.CompleteAsync(id, Actor(), ct));

    [HttpPost("transactions/{id:guid}/reject")]
    public Task<ActionResult<TransactionResponse>> Reject(Guid id, CancellationToken ct) =>
        Execute(() => service.RejectAsync(id, Actor(), ct));

    private async Task<IActionResult> DecideOffer(Guid id, OfferStatus status, CancellationToken ct)
    {
        try { await service.DecideOfferAsync(id, Actor(), status, ct); return Ok(); }
        catch (TransactionOperationException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }

    private Guid Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw new TransactionOperationException(401, "A valid manager identity is required.");

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (TransactionOperationException ex) { return StatusCode(ex.StatusCode, new ProblemDetails { Status = ex.StatusCode, Detail = ex.Message }); }
    }
}
