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
    // APPROVED is the existing persisted waiting-for-seller state. Keeping the
    // name avoids a breaking change for current clients and historic records.
    APPROVED,
    REJECTED,
    COMPLETED,
    // HANDED_OVER is the existing persisted waiting-for-buyer state.
    HANDED_OVER,
    NOT_COMPLETED,
    MANAGER_REVIEW_REQUIRED
}

public sealed class Offer : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid MaterialMatchId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Quantity { get; set; }
    public int? PackageCount { get; set; }
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
    // New selections are explicitly grouped by their buyer-confirmed workflow.
    // Null preserves historical single-match transaction records.
    public Guid? ApprovalWorkflowId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Quantity { get; set; }
    public int? PackageCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal ReservedQuantity { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime? ManagerApprovedAtUtc { get; set; }
    public DateTime? ConfirmationDeadlineUtc { get; set; }
    public DateTime? SellerHandoverConfirmedAtUtc { get; set; }
    public DateTime? BuyerReceivedConfirmedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? ResolvedByManagerId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNote { get; set; }
    public string? ResolutionReasonCode { get; set; }
    public DateTime? FollowUpNotifiedAtUtc { get; set; }
    public uint Version { get; set; }
    public Offer Offer { get; set; } = null!;
    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
}
