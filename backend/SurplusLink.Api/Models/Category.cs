namespace SurplusLink.Api.Models;

public sealed class Category : AuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
