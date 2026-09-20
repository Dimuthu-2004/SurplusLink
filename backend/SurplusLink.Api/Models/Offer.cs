namespace SurplusLink.Api.Models;

public enum OfferStatus
{
    PENDING,
    ACCEPTED,
    REJECTED,
    REVISION_REQUESTED
}

public enum TransactionStatus
{
    PENDING_APPROVAL,
    APPROVED,
    REJECTED,
    COMPLETED
}

public sealed class Offer : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid MaterialMatchId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
    public OfferStatus Status { get; set; }
    public MaterialMatch MaterialMatch { get; set; } = null!;
    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
}

public sealed class Transaction : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal ReservedQuantity { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public uint Version { get; set; }
    public Offer Offer { get; set; } = null!;
    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
}
