# Server-side routing and transport estimates

ASP.NET Core owns the routing adapter. Inject `IRoutingProvider` for route metrics
or `ITransportEstimateService` for metrics plus a transport estimate. Both take
`RouteRequest(sellerLatitude, sellerLongitude, buyerLatitude, buyerLongitude)`.
No frontend/provider endpoint, key, or direct client integration is added.

The first implementation uses the OpenRouteService POST directions JSON API:
https://giscience.github.io/openrouteservice/api-reference/endpoints/directions/requests-and-return-types
It sends longitude/latitude coordinate pairs and converts summary distance from
meters to `distanceKm`, and duration from seconds to `durationMinutes`.

Configure these environment variables on the ASP.NET Core host only:

```text
Routing__Provider=OpenRouteService
Routing__Endpoint=https://api.openrouteservice.org/v2/directions/driving-car/json
Routing__ApiKey=<server-side provider key>
Routing__TimeoutSeconds=10
Routing__BaseFee=<LKR base fee>
Routing__CostPerKm=<LKR per kilometer>
Routing__CostPerMinute=<LKR per minute; set 0 if unused>
```

Registration reads only environment variables with the `Routing__` prefix, not
appsettings, user secrets, or client configuration. Missing provider settings
return `ROUTING_NOT_CONFIGURED`. Timeout defaults to 10 seconds and must be in
(0, 120]. Pricing values must all be present and nonnegative. The estimate is
`BaseFee + distanceKm * CostPerKm + durationMinutes * CostPerMinute`, rounded once
to two decimal places, midpoint away from zero. It is an estimate, not a carrier
quote. Missing pricing returns metrics with `TRANSPORT_PRICING_NOT_CONFIGURED`
and no cost. There are no invented default tariffs.

Controlled failures return null metrics and an error code: INVALID_COORDINATES,
ROUTING_TIMEOUT, ROUTING_RATE_LIMITED, ROUTING_INVALID_RESPONSE, or
ROUTING_UNAVAILABLE. No provider body, key, or exception message is returned.
429 includes Retry-After when supplied (seconds or HTTP date, clamped to 0–86400
seconds), with no automatic retry that might amplify a quota problem. Caller
cancellation propagates; provider timeout is a safe result. Failed routing
never produces a zero-price estimate or a straight-line substitute.

The HttpClient is created through DI, limits buffered responses to 64 KiB,
redacts headers in HttpClient logging, and disables redirects to avoid forwarding
credentials. The endpoint must be HTTPS without credentials, query, or fragment.
The API key is sent in the Authorization header, never in a URL or response DTO.

To change provider, implement `IRoutingProvider` and replace its DI registration;
the estimator and callers depend only on the interface. Replace the interface
with a fake in tests, or supply an HttpMessageHandler to test the HTTP adapter.
The current branch has no match logistics service; a future workflow should
invoke this service and persist the result through its own match action boundary.

Run `dotnet test backend/SurplusLink.Tests --filter RoutingProviderTests` for
normal routing, timeout, rate limiting, invalid payloads, upstream failures,
cancellation, configuration, credential handling, pricing and provider swapping.
No live provider credentials or network calls are needed by these tests.
