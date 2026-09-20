using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SurplusLink.Tests;

public sealed class SharedApiQualityTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Unhandled_exception_returns_safe_problem_details()
    {
        using var response = await _client.GetAsync("/_tests/exception");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("Sensitive exception detail", body.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_model_returns_validation_problem_details()
    {
        using var response = await _client.PostAsJsonAsync("/_tests/validation", new { name = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Unknown_route_returns_standard_problem_details()
    {
        using var response = await _client.GetAsync("/route-that-does-not-exist");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Theory]
    [InlineData(ApiWebApplicationFactory.AllowedOrigin, true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Cors_allows_only_configured_origins(string origin, bool shouldBeAllowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await _client.SendAsync(request);
        var allowedOrigin = response.Headers.TryGetValues(
            "Access-Control-Allow-Origin",
            out var values)
            ? values.SingleOrDefault()
            : null;

        Assert.Equal(shouldBeAllowed ? origin : null, allowedOrigin);
    }

    [Fact]
    public async Task Swagger_describes_jwt_bearer_authentication()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bearer = document
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
        Assert.Contains(document.GetProperty("security").EnumerateArray(), item =>
            item.TryGetProperty("Bearer", out _));
        var paths = document.GetProperty("paths");
        foreach (var path in new[]
        {
            "/api/offers", "/api/transactions", "/api/transactions/{id}/history",
            "/api/transactions/analytics/summary", "/api/transactions/{id}/approve",
            "/api/transactions/{id}/reject", "/api/transactions/{id}/complete"
        })
            Assert.True(paths.TryGetProperty(path, out _), path);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
