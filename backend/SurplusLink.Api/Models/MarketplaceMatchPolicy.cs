namespace SurplusLink.Api.Models;

public static class MarketplaceMatchPolicy
{
    public const string SelfMatchNotAllowed = "SELF_MATCH_NOT_ALLOWED";

    // Future candidate queries must also filter Listing.SellerId != BuyerRequest.BuyerId.
    public static string? RejectionReason(Guid buyerId, Guid sellerId) =>
        buyerId == sellerId ? SelfMatchNotAllowed : null;
}
