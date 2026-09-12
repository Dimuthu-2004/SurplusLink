namespace SurplusLink.Api.Models;

public sealed class MarketplaceTransaction : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid ReservationId { get; set; }

    public TransactionStatus Status { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
