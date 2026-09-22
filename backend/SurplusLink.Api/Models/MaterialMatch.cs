namespace SurplusLink.Api.Models;

public sealed class MaterialMatch : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid MaterialRequestId { get; set; }

    public Guid ListingId { get; set; }

    public decimal Score { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.GENERATED;

    public decimal? Distance { get; set; }
    public decimal? DurationMinutes { get; set; }

    public decimal? EstimatedTransportCost { get; set; }

    public string? RejectionReason { get; set; }

    public uint Version { get; set; }

    public BuyerRequest MaterialRequest { get; set; } = null!;

    public Listing Listing { get; set; } = null!;
}

public enum MatchStatus { GENERATED, RANKED, ROUTED, ROUTE_FAILED, REJECTED }
