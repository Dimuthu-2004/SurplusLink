using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SurplusLink.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:5173";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SurplusLink"] =
                    "Host=localhost;Database=surpluslink_tests;Username=test;Password=test",
                ["Jwt:Issuer"] = "SurplusLink.Api.Tests",
                ["Jwt:Audience"] = "SurplusLink.Tests",
                ["Jwt:Secret"] = "test-only-secret-with-at-least-thirty-two-characters",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Cors:AllowedOrigins:0"] = AllowedOrigin
            });
        });
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestEndpointsController).Assembly));
    }
}
