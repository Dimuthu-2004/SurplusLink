namespace SurplusLink.Api.Locations;

public sealed record ReverseGeocodingResponse(
    decimal Latitude,
    decimal Longitude,
    string DisplayName,
    string Source);

public sealed class ReverseGeocodingUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

public interface IReverseGeocodingService
{
    Task<ReverseGeocodingResponse> ReverseAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken);
}