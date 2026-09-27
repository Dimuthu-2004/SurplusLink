namespace SurplusLink.Api.Models;

public enum NotificationContext
{
    BUYER,
    SELLER,
    MANAGER,
    SYSTEM
}

public enum NotificationPriority
{
    INFO,
    SUCCESS,
    ACTION_REQUIRED,
    WARNING,
    CRITICAL
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationContext Context { get; set; }
    public NotificationPriority Priority { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? ActionRoute { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public string? DeduplicationKey { get; set; }
    public User User { get; set; } = null!;
}
