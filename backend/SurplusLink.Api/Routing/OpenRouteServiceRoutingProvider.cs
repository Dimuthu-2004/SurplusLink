using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SurplusLink.Api.Routing;

public sealed class OpenRouteServiceRoutingProvider(HttpClient client, RoutingOptions options) : IRoutingProvider
{
    public async Task<RouteResult> GetRouteAsync(RouteRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.IsValid) return RouteResult.Failure("INVALID_COORDINATES");
        if (!options.CanRoute) return RouteResult.Failure("ROUTING_NOT_CONFIGURED");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        // Keep credentials out of URLs, payloads, exceptions and response DTOs.
        message.Headers.TryAddWithoutValidation("Authorization", options.ApiKey);
        message.Content = JsonContent.Create(new
        {
            coordinates = new[]
            {
                new[] { request.SellerLongitude, request.SellerLatitude },
                new[] { request.BuyerLongitude, request.BuyerLatitude }
            },
            units = "m", geometry = false, instructions = false
        });
        try
        {
            using var response = await client.SendAsync(message, deadline.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return RouteResult.Failure("ROUTING_RATE_LIMITED", RetryAfter(response));
            if (!response.IsSuccessStatusCode) return RouteResult.Failure("ROUTING_UNAVAILABLE");
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
            if (json.RootElement.ValueKind != JsonValueKind.Object
                || !json.RootElement.TryGetProperty("routes", out var routes)
                || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0
                || routes[0].ValueKind != JsonValueKind.Object
                || !routes[0].TryGetProperty("summary", out var summary)
                || summary.ValueKind != JsonValueKind.Object
                || !Number(summary, "distance", out var distance)
                || !Number(summary, "duration", out var duration))
                return RouteResult.Failure("ROUTING_INVALID_RESPONSE");
            return new(distance / 1000m, duration / 60m);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RouteResult.Failure("ROUTING_TIMEOUT");
        }
        catch (JsonException) { return RouteResult.Failure("ROUTING_INVALID_RESPONSE"); }
        catch (HttpRequestException) { return RouteResult.Failure("ROUTING_UNAVAILABLE"); }
        catch (IOException) { return RouteResult.Failure("ROUTING_UNAVAILABLE"); }
    }

    private static bool Number(JsonElement summary, string name, out decimal number)
    {
        number = 0;
        return summary.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetDecimal(out number) && number >= 0;
    }

    private static int? RetryAfter(HttpResponseMessage response)
    {
        var retry = response.Headers.RetryAfter;
        var delay = retry?.Delta ?? (retry?.Date - DateTimeOffset.UtcNow);
        return delay.HasValue ? (int)Math.Clamp(Math.Ceiling(delay.Value.TotalSeconds), 0, 86400) : null;
    }
}
