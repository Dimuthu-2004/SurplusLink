using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SurplusLink.Tests;

public sealed class AnalyticsEndpointContractTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/api/materials/analytics/summary")]
    [InlineData("/api/requirements/analytics/summary")]
    [InlineData("/api/matches/analytics/summary")]
    [InlineData("/api/transactions/analytics/summary")]
    public async Task Analytics_endpoints_require_authentication(string endpoint)
    {
        using var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_contains_every_component_analytics_endpoint()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/materials/analytics/summary", out _));
        Assert.True(paths.TryGetProperty("/api/requirements/analytics/summary", out _));
        Assert.True(paths.TryGetProperty("/api/matches/analytics/summary", out _));
        Assert.True(paths.TryGetProperty("/api/transactions/analytics/summary", out _));
    }
}
