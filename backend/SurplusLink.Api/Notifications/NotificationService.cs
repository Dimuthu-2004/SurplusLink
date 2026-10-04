using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Notifications;

public sealed class NotificationService(SurplusLinkDbContext db, IHubContext<NotificationHub>? hubContext = null) : INotificationService
{
    public async Task<NotificationResponse> CreateAsync(
        CreateNotification input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var deduplicationKey = CleanOptional(input.DeduplicationKey);
        if (deduplicationKey is not null)
        {
            var existing = await db.Notifications.AsNoTracking().FirstOrDefaultAsync(
                item => item.UserId == input.UserId && item.DeduplicationKey == deduplicationKey,
                cancellationToken);
            if (existing is not null) return ToResponse(existing);
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = input.UserId,
            Type = input.Type.Trim(),
            Title = input.Title.Trim(),
            Message = input.Message.Trim(),
            Context = input.Context,
            Priority = input.Priority,
            EntityType = CleanOptional(input.EntityType),
            EntityId = input.EntityId,
            ActionRoute = CleanOptional(input.ActionRoute),
            DeduplicationKey = deduplicationKey,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Notifications.Add(notification);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            var response = ToResponse(notification);
            if (hubContext is not null)
            {
                try
                {
                    await hubContext.Clients.User(notification.UserId.ToString())
                        .SendAsync("ReceiveNotification", response, cancellationToken);
                }
                catch
                {
                    // Real-time broadcast failure should not break notification persistence
                }
            }
            return response;
        }
        catch (DbUpdateException) when (deduplicationKey is not null)
        {
            db.Entry(notification).State = EntityState.Detached;
            var existing = await db.Notifications.AsNoTracking().SingleOrDefaultAsync(
                item => item.UserId == input.UserId && item.DeduplicationKey == deduplicationKey,
                cancellationToken);
            if (existing is not null) return ToResponse(existing);
            throw;
        }
    }

    public async Task<NotificationPage> ListAsync(
        Guid userId,
        NotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = db.Notifications.AsNoTracking().Where(item => item.UserId == userId);
        if (query.Unread.HasValue) rows = rows.Where(item => item.IsRead != query.Unread.Value);
        if (query.Context.HasValue) rows = rows.Where(item => item.Context == query.Context.Value);
        if (query.Priority.HasValue) rows = rows.Where(item => item.Priority == query.Priority.Value);

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);
        return new(items, total, query.Page, query.PageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);

    public async Task<NotificationResponse> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.UserId == userId,
            cancellationToken) ?? throw new NotificationException(404, "Notification was not found.");
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return ToResponse(notification);
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await db.Notifications.Where(item => item.UserId == userId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAtUtc, now), cancellationToken);
    }

    private static void Validate(CreateNotification input)
    {
        if (input.UserId == Guid.Empty) throw new ArgumentException("A notification user is required.", nameof(input));
        Required(input.Type, 80, nameof(input.Type));
        Required(input.Title, 160, nameof(input.Title));
        Required(input.Message, 1_000, nameof(input.Message));
        Optional(input.EntityType, 80, nameof(input.EntityType));
        Optional(input.ActionRoute, 500, nameof(input.ActionRoute));
        Optional(input.DeduplicationKey, 200, nameof(input.DeduplicationKey));
    }

    private static void Required(string value, int maximum, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximum)
            throw new ArgumentException($"{name} is required and must be at most {maximum} characters.", name);
    }

    private static void Optional(string? value, int maximum, string name)
    {
        if (value?.Trim().Length > maximum)
            throw new ArgumentException($"{name} must be at most {maximum} characters.", name);
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotificationResponse ToResponse(Notification item) => new(
        item.Id, item.Type, item.Title, item.Message, item.Context, item.Priority,
        item.EntityType, item.EntityId, item.ActionRoute, item.IsRead, item.CreatedAtUtc, item.ReadAtUtc);
}

public sealed class NotificationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
