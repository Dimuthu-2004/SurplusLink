using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SurplusLink.Api.Locations;

namespace SurplusLink.Tests;

public sealed class LocationReverseGeocodingTests
{
    [Fact]
    public async Task Reverse_returns_display_name_and_reuses_cached_coordinates()
    {
        var handler = new FakeHandler("{\"display_name\":\"Colombo, Sri Lanka\"}");
        var service = CreateService(handler);

        var first = await service.ReverseAsync(6.927079m, 79.861244m, CancellationToken.None);
        var second = await service.ReverseAsync(6.927079m, 79.861244m, CancellationToken.None);

        Assert.Equal("Colombo, Sri Lanka", first.DisplayName);
        Assert.Equal("OpenStreetMap Nominatim", first.Source);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("format=jsonv2", handler.LastUri!.Query);
        Assert.Contains("lat=6.927079", handler.LastUri.Query);
        Assert.Contains("lon=79.861244", handler.LastUri.Query);
    }

    [Fact]
    public async Task Reverse_converts_provider_failures_to_controlled_exception()
    {
        var service = CreateService(new FakeHandler(throwOnRequest: true));

        var error = await Assert.ThrowsAsync<ReverseGeocodingUnavailableException>(() =>
            service.ReverseAsync(6, 79, CancellationToken.None));

        Assert.Contains("temporarily unavailable", error.Message);
    }

    [Fact]
    public async Task Controller_rejects_coordinates_outside_allowed_ranges()
    {
        var controller = new LocationsController(new ThrowingService());

        var latitude = await controller.Reverse(91, 0, CancellationToken.None);
        var longitude = await controller.Reverse(0, 181, CancellationToken.None);

        Assert.Equal(400, ((ObjectResult)latitude.Result!).StatusCode);
        Assert.Equal(400, ((ObjectResult)longitude.Result!).StatusCode);
    }

    private static NominatimReverseGeocodingService CreateService(FakeHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/reverse") };
        return new(client, new MemoryCache(new MemoryCacheOptions()), NullLogger<NominatimReverseGeocodingService>.Instance);
    }

    private sealed class FakeHandler(string response = "{}", bool throwOnRequest = false) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastUri = request.RequestUri;
            if (throwOnRequest) throw new HttpRequestException("offline");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowingService : IReverseGeocodingService
    {
        public Task<ReverseGeocodingResponse> ReverseAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken) =>
            throw new InvalidOperationException();
    }
}