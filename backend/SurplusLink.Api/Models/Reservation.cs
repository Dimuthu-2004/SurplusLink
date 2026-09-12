namespace SurplusLink.Api.Models;

public sealed class Reservation : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public Guid MaterialRequestId { get; set; }

    public decimal Quantity { get; set; }

    public ReservationStatus Status { get; set; }

    public Listing Listing { get; set; } = null!;

    public MaterialRequest MaterialRequest { get; set; } = null!;
}
