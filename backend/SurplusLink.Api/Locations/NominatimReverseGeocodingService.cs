using System.Net;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace SurplusLink.Api.Locations;

public sealed class NominatimReverseGeocodingService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<NominatimReverseGeocodingService> logger) : IReverseGeocodingService
{
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset lastRequest = DateTimeOffset.MinValue;
    private static readonly TimeSpan MinimumRequestInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<ReverseGeocodingResponse> ReverseAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken)
    {
        var key = FormattableString.Invariant($"nominatim:{latitude:F6}:{longitude:F6}");
        if (cache.TryGetValue(key, out ReverseGeocodingResponse? cached) && cached is not null)
            return cached;

        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var wait = MinimumRequestInterval - (DateTimeOffset.UtcNow - lastRequest);
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);

            var uri = new UriBuilder(httpClient.BaseAddress!)
            {
                Query = string.Create(
                    CultureInfo.InvariantCulture,
                    $"format=jsonv2&lat={latitude:F6}&lon={longitude:F6}")
            }.Uri;
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            lastRequest = DateTimeOffset.UtcNow;
            if (response.StatusCode is not HttpStatusCode.OK)
                throw new ReverseGeocodingUnavailableException("OpenStreetMap address lookup is temporarily unavailable.");

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<NominatimResponse>(content, cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(payload?.DisplayName))
                throw new ReverseGeocodingUnavailableException("OpenStreetMap did not return an address for this position.");

            var result = new ReverseGeocodingResponse(
                latitude,
                longitude,
                payload.DisplayName,
                "OpenStreetMap Nominatim");
            cache.Set(key, result, CacheDuration);
            return result;
        }
        catch (ReverseGeocodingUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Nominatim reverse geocoding timed out.");
            throw new ReverseGeocodingUnavailableException("OpenStreetMap address lookup timed out.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "Nominatim reverse geocoding failed.");
            throw new ReverseGeocodingUnavailableException("OpenStreetMap address lookup is temporarily unavailable.", exception);
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private sealed record NominatimResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("display_name")] string? DisplayName);
}