using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SurplusLink.Api.Notifications;

[Authorize]
public sealed class NotificationHub : Hub
{
    // Clients listen for "ReceiveNotification" event sent by server to their user ID
}
