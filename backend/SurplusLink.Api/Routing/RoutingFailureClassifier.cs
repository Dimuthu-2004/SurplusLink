namespace SurplusLink.Api.Routing;

/// <summary>Maps trusted routing inputs/results to stable, safe match reasons.</summary>
public static class RoutingFailureClassifier
{
    public static string? ValidateCoordinates(
        decimal? sellerLatitude, decimal? sellerLongitude,
        decimal? buyerLatitude, decimal? buyerLongitude)
    {
        if (sellerLatitude is null || sellerLongitude is null) return "MISSING_SELLER_LOCATION";
        if (buyerLatitude is null || buyerLongitude is null) return "MISSING_BUYER_LOCATION";
        if (!ValidLatitude(sellerLatitude.Value) || !ValidLongitude(sellerLongitude.Value) || (sellerLatitude == 0 && sellerLongitude == 0)) return "INVALID_SELLER_COORDINATES";
        if (!ValidLatitude(buyerLatitude.Value) || !ValidLongitude(buyerLongitude.Value) || (buyerLatitude == 0 && buyerLongitude == 0)) return "INVALID_BUYER_COORDINATES";
        return null;
    }

    public static string FromEstimate(TransportEstimate? estimate) => (estimate?.ErrorCode ?? estimate?.Route.ErrorCode) switch
    {
        "ROUTING_TIMEOUT" => "ROUTING_TIMEOUT",
        "ROUTING_INVALID_RESPONSE" => "MALFORMED_ROUTING_RESPONSE",
        "NO_ROUTE_FOUND" => "NO_ROUTE_FOUND",
        "ROUTING_UNAVAILABLE" or "ROUTING_RATE_LIMITED" or "ROUTING_NOT_CONFIGURED" or "TRANSPORT_PRICING_NOT_CONFIGURED" => "ROUTING_PROVIDER_ERROR",
        _ => "ROUTING_PROVIDER_ERROR"
    };

    private static bool ValidLatitude(decimal value) => value is >= -90 and <= 90;
    private static bool ValidLongitude(decimal value) => value is >= -180 and <= 180;
}
