namespace SurplusLink.Api.Models;

// Uses the existing MaterialRequests table; matching/reservation foreign keys stay intact.
public sealed class BuyerRequest : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid BuyerId { get; set; }

    public Guid CategoryId { get; set; }

    // Retained for legacy rows. New requests receive a category-based title internally.
    public string Title { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public decimal RequiredQuantity { get; set; }

    public decimal MaximumBudget { get; set; }

    public DateTime Deadline { get; set; }

    public BuyerRequestStatus Status { get; set; }

    public string Unit { get; set; } = "unit";

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public uint Version { get; set; }

    public User Buyer { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
