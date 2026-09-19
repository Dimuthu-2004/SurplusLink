namespace SurplusLink.Api.Routing;

public sealed record RouteRequest(decimal SellerLatitude, decimal SellerLongitude,
    decimal BuyerLatitude, decimal BuyerLongitude)
{
    public bool IsValid => SellerLatitude is >= -90 and <= 90 && BuyerLatitude is >= -90 and <= 90
        && SellerLongitude is >= -180 and <= 180 && BuyerLongitude is >= -180 and <= 180;
}

public sealed record RouteResult(decimal? DistanceKm, decimal? DurationMinutes,
    string? ErrorCode = null, int? RetryAfterSeconds = null)
{
    public bool Success => ErrorCode is null && DistanceKm is >= 0 && DurationMinutes is >= 0;
    public static RouteResult Failure(string code, int? retryAfter = null) => new(null, null, code, retryAfter);
}

public interface IRoutingProvider
{
    Task<RouteResult> GetRouteAsync(RouteRequest request, CancellationToken cancellationToken);
}

public sealed record TransportEstimate(RouteResult Route, decimal? EstimatedTransportCost,
    string Currency = "LKR", string? ErrorCode = null);

public interface ITransportEstimateService
{
    Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken cancellationToken);
}
