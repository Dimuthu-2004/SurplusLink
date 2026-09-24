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

    [Fact]
    public async Task Search_returns_coordinates_and_caches_the_address()
    {
        var handler = new FakeHandler("""[{"lat":"6.9","lon":"79.8","display_name":"Colombo"}]""");
        var service = CreateService(handler);
        var first = await service.SearchAsync(" Colombo ", CancellationToken.None);
        Assert.Equal(6.9m, Assert.Single(first).Latitude);
        Assert.Equal(79.8m, first[0].Longitude);
        Assert.Equal("Colombo", first[0].DisplayName);
        Assert.Equal(first, await service.SearchAsync("colombo", CancellationToken.None));
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("/search", handler.LastUri!.AbsolutePath);
        Assert.Contains("q=Colombo", handler.LastUri.Query);
    }

    [Fact]
    public async Task Search_empty_results_are_successful_and_failures_are_controlled()
    {
        Assert.Empty(await CreateService(new FakeHandler("[]")).SearchAsync("No place", CancellationToken.None));
        await Assert.ThrowsAsync<ReverseGeocodingUnavailableException>(() => CreateService(new FakeHandler(throwOnRequest: true)).SearchAsync("Colombo", CancellationToken.None));
        var invalid = await new LocationsController(new ThrowingService()).Search(" ", CancellationToken.None);
        Assert.Equal(400, ((ObjectResult)invalid.Result!).StatusCode);
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
        public Task<IReadOnlyList<ReverseGeocodingResponse>> SearchAsync(string query, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public Task<ReverseGeocodingResponse> ReverseAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken) =>
            throw new InvalidOperationException();
    }
}