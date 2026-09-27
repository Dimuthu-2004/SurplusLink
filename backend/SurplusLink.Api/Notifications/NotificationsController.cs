using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Notifications;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationPage), 200)]
    public Task<IActionResult> List([FromQuery] NotificationQuery query, CancellationToken cancellationToken) =>
        Handle(async userId => Ok(await service.ListAsync(userId, query, cancellationToken)));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCount), 200)]
    public Task<IActionResult> UnreadCount(CancellationToken cancellationToken) =>
        Handle(async userId => Ok(new UnreadNotificationCount(
            await service.UnreadCountAsync(userId, cancellationToken))));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    public Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Handle(async userId => Ok(await service.MarkReadAsync(userId, id, cancellationToken)));

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(UnreadNotificationCount), 200)]
    public Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        Handle(async userId =>
        {
            await service.MarkAllReadAsync(userId, cancellationToken);
            return Ok(new UnreadNotificationCount(0));
        });

    private async Task<IActionResult> Handle(Func<Guid, Task<IActionResult>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
            return Unauthorized();
        try { return await action(userId); }
        catch (NotificationException exception)
        {
            return Problem(statusCode: exception.StatusCode, detail: exception.Message);
        }
    }
}
