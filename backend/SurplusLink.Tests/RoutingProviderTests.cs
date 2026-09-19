using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SurplusLink.Api.Routing;

namespace SurplusLink.Tests;

public sealed class RoutingProviderTests
{
    private static readonly RouteRequest Request = new(6.9271m, 79.8612m, 7.2906m, 80.6337m);
    private static RoutingOptions Options() => new()
    {
        Endpoint = "https://api.openrouteservice.org/v2/directions/driving-car/json",
        ApiKey = "test-server-only-key", BaseFee = 100, CostPerKm = 50, CostPerMinute = 2
    };

    [Fact]
    public async Task Normal_route_converts_units_uses_header_and_calculates_transport_cost()
    {
        var options = Options();
        var handler = new Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(options.Endpoint, request.RequestUri!.AbsoluteUri);
            Assert.Equal(options.ApiKey, Assert.Single(request.Headers.GetValues("Authorization")));
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.DoesNotContain(options.ApiKey!, body);
            using var json = JsonDocument.Parse(body);
            var coordinates = json.RootElement.GetProperty("coordinates");
            Assert.Equal(Request.SellerLongitude, coordinates[0][0].GetDecimal());
            Assert.Equal(Request.SellerLatitude, coordinates[0][1].GetDecimal());
            Assert.Equal(Request.BuyerLongitude, coordinates[1][0].GetDecimal());
            Assert.Equal(Request.BuyerLatitude, coordinates[1][1].GetDecimal());
            return Response("""{"routes":[{"summary":{"distance":12500,"duration":1800}}]}""");
        });
        using var client = new HttpClient(handler);
        var service = new TransportEstimateService(new OpenRouteServiceRoutingProvider(client, options), options);
        var result = await service.EstimateAsync(Request, default);
        Assert.True(result.Route.Success);
        Assert.Equal(12.5m, result.Route.DistanceKm);
        Assert.Equal(30m, result.Route.DurationMinutes);
        Assert.Equal(785m, result.EstimatedTransportCost);
        Assert.Equal("LKR", result.Currency);
        Assert.DoesNotContain(options.ApiKey!, JsonSerializer.Serialize(result));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Timeout_returns_safe_failure_without_a_fabricated_estimate()
    {
        var options = Options();
        options.TimeoutSeconds = .03;
        var handler = new Handler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return Response("{}");
        });
        using var client = new HttpClient(handler);
        var service = new TransportEstimateService(new OpenRouteServiceRoutingProvider(client, options), options);
        var result = await service.EstimateAsync(Request, default);
        Assert.Equal("ROUTING_TIMEOUT", result.ErrorCode);
        Assert.Null(result.Route.DistanceKm);
        Assert.Null(result.Route.DurationMinutes);
        Assert.Null(result.EstimatedTransportCost);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Rate_limit_returns_retry_after_without_retrying_or_exposing_provider_body()
    {
        var options = Options();
        var handler = new Handler((_, _) =>
        {
            var response = Response(options.ApiKey!, HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            return Task.FromResult(response);
        });
        using var client = new HttpClient(handler);
        var result = await new OpenRouteServiceRoutingProvider(client, options).GetRouteAsync(Request, default);
        Assert.Equal("ROUTING_RATE_LIMITED", result.ErrorCode);
        Assert.Equal(45, result.RetryAfterSeconds);
        Assert.False(result.Success);
        Assert.DoesNotContain(options.ApiKey!, JsonSerializer.Serialize(result));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"routes\":[]}")]
    [InlineData("{\"routes\":[{\"summary\":{\"distance\":-1,\"duration\":60}}]}")]
    [InlineData("{\"routes\":[{\"summary\":{\"distance\":1000}}]}")]
    [InlineData("{\"routes\":[{\"summary\":{\"distance\":\"1000\",\"duration\":60}}]}")]
    public async Task Invalid_payload_returns_safe_failure(string body)
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Response(body))));
        var result = await new OpenRouteServiceRoutingProvider(client, Options()).GetRouteAsync(Request, default);
        Assert.Equal("ROUTING_INVALID_RESPONSE", result.ErrorCode);
        Assert.Null(result.DistanceKm);
        Assert.Null(result.DurationMinutes);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(302)]
    public async Task Provider_failure_does_not_echo_body(int status)
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Response("secret provider diagnostic", (HttpStatusCode)status))));
        var result = await new OpenRouteServiceRoutingProvider(client, Options()).GetRouteAsync(Request, default);
        Assert.Equal("ROUTING_UNAVAILABLE", result.ErrorCode);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task Network_errors_are_safe_and_caller_cancellation_is_preserved()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new HttpRequestException("secret")));
        var provider = new OpenRouteServiceRoutingProvider(client, Options());
        Assert.Equal("ROUTING_UNAVAILABLE", (await provider.GetRouteAsync(Request, default)).ErrorCode);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRouteAsync(Request, cancellation.Token));
    }

    [Fact]
    public async Task Missing_configuration_and_invalid_coordinates_make_no_http_call()
    {
        var handler = new Handler((_, _) => throw new InvalidOperationException("Must not call provider"));
        using var client = new HttpClient(handler);
        Assert.Equal("ROUTING_NOT_CONFIGURED", (await new OpenRouteServiceRoutingProvider(client, new()).GetRouteAsync(Request, default)).ErrorCode);
        Assert.Equal("INVALID_COORDINATES", (await new OpenRouteServiceRoutingProvider(client, Options()).GetRouteAsync(Request with { BuyerLatitude = 91 }, default)).ErrorCode);
        var insecure = Options();
        insecure.Endpoint = "http://example.com";
        Assert.Equal("ROUTING_NOT_CONFIGURED", (await new OpenRouteServiceRoutingProvider(client, insecure).GetRouteAsync(Request, default)).ErrorCode);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Provider_is_swappable_and_missing_pricing_does_not_invent_a_quote()
    {
        var services = new ServiceCollection();
        services.AddRoutingProvider();
        services.AddSingleton<IRoutingProvider>(new FakeProvider());
        services.AddSingleton(new RoutingOptions { BaseFee = 1, CostPerKm = .111m, CostPerMinute = 0 });
        using var container = services.BuildServiceProvider();
        var estimate = await container.GetRequiredService<ITransportEstimateService>().EstimateAsync(Request, default);
        Assert.Equal(1.56m, estimate.EstimatedTransportCost);
        var unpriced = await new TransportEstimateService(new FakeProvider(), new()).EstimateAsync(Request, default);
        Assert.Equal("TRANSPORT_PRICING_NOT_CONFIGURED", unpriced.ErrorCode);
        Assert.Null(unpriced.EstimatedTransportCost);
    }

    private sealed class FakeProvider : IRoutingProvider
    {
        public Task<RouteResult> GetRouteAsync(RouteRequest request, CancellationToken cancellationToken) => Task.FromResult(new RouteResult(5, 10));
    }

    private static HttpResponseMessage Response(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return send(request, cancellationToken);
        }
    }
}
