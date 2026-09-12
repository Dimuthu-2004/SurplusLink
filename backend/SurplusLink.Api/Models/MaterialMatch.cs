namespace SurplusLink.Api.Models;

public sealed class MaterialMatch : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid MaterialRequestId { get; set; }

    public Guid ListingId { get; set; }

    public decimal Score { get; set; }

    public MaterialRequest MaterialRequest { get; set; } = null!;

    public Listing Listing { get; set; } = null!;
}
