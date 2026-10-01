namespace SurplusLink.Api.Models;

public sealed class Reservation : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public Guid MaterialRequestId { get; set; }

    // New reservations are owned by one allocation transaction. Nullable keeps
    // historical reservations readable until they are naturally resolved.
    public Guid? TransactionId { get; set; }

    public decimal Quantity { get; set; }
    public int? PackageCount { get; set; }

    public ReservationStatus Status { get; set; }

    public Listing Listing { get; set; } = null!;

    public BuyerRequest MaterialRequest { get; set; } = null!;
}
