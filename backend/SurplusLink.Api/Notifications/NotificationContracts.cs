using System.ComponentModel.DataAnnotations;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Notifications;

public sealed class NotificationQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public bool? Unread { get; init; }
    public NotificationContext? Context { get; init; }
    public NotificationPriority? Priority { get; init; }
}

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    NotificationContext Context,
    NotificationPriority Priority,
    string? EntityType,
    Guid? EntityId,
    string? ActionRoute,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record NotificationPage(
    IReadOnlyList<NotificationResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record UnreadNotificationCount(int Count);

public sealed record CreateNotification(
    Guid UserId,
    string Type,
    string Title,
    string Message,
    NotificationContext Context,
    NotificationPriority Priority,
    string? EntityType = null,
    Guid? EntityId = null,
    string? ActionRoute = null,
    string? DeduplicationKey = null);
