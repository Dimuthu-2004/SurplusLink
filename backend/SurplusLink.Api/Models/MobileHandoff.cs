namespace SurplusLink.Api.Models;

public sealed class MobileHandoff : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Source { get; set; } = "REACT_MARKETPLACE";
    public DateTime ExpiresAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public Guid? RedeemedByUserId { get; set; }
    public User? RedeemedByUser { get; set; }
}
