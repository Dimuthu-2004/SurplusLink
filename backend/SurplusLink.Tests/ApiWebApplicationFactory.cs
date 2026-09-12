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
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SurplusLink"] =
                    "Host=localhost;Database=surpluslink_tests;Username=test;Password=test",
                ["Authentication:JwtBearer:Authority"] = "https://identity.surpluslink.test",
                ["Authentication:JwtBearer:Audience"] = "surpluslink-api"
            });
        });
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestEndpointsController).Assembly));
    }
}
