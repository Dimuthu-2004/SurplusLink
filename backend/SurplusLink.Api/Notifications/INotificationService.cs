namespace SurplusLink.Api.Notifications;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(CreateNotification input, CancellationToken cancellationToken = default);
    Task<NotificationPage> ListAsync(Guid userId, NotificationQuery query, CancellationToken cancellationToken = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationResponse> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
