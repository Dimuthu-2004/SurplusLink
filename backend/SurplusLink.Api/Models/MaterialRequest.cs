namespace SurplusLink.Api.Models;

public sealed class MaterialRequest : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid BuyerId { get; set; }

    public Guid CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Budget { get; set; }

    public DateTime DeadlineUtc { get; set; }

    public MaterialRequestStatus Status { get; set; }

    public User Buyer { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
