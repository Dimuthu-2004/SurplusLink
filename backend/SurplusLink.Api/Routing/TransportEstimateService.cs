namespace SurplusLink.Api.Routing;

public sealed class TransportEstimateService(IRoutingProvider provider, RoutingOptions options) : ITransportEstimateService
{
    public async Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken cancellationToken)
    {
        var route = await provider.GetRouteAsync(request, cancellationToken);
        if (!route.Success) return new(route, null, ErrorCode: route.ErrorCode ?? "ROUTING_INVALID_RESPONSE");
        if (!options.CanEstimate) return new(route, null, ErrorCode: "TRANSPORT_PRICING_NOT_CONFIGURED");
        try
        {
            var cost = options.BaseFee!.Value + route.DistanceKm!.Value * options.CostPerKm!.Value
                + route.DurationMinutes!.Value * options.CostPerMinute!.Value;
            return new(route, decimal.Round(cost, 2, MidpointRounding.AwayFromZero));
        }
        catch (OverflowException) { return new(route, null, ErrorCode: "TRANSPORT_ESTIMATE_OUT_OF_RANGE"); }
    }
}
