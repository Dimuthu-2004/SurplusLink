namespace SurplusLink.Api.Models;

public sealed class AuditLog : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public User? ActorUser { get; set; }
}
