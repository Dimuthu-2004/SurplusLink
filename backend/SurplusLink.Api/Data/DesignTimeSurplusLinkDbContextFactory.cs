using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SurplusLink.Api.Data;

public sealed class DesignTimeSurplusLinkDbContextFactory : IDesignTimeDbContextFactory<SurplusLinkDbContext>
{
    public SurplusLinkDbContext CreateDbContext(string[] args)
    {
        // EF tooling does not use launchSettings.json. Default local commands to
        // Development so they can use the same user secrets as the API.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ApplicationName = typeof(DesignTimeSurplusLinkDbContextFactory).Assembly.GetName().Name,
            EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environments.Development
        });
        var connectionString = builder.Configuration.GetConnectionString("SurplusLink");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:SurplusLink in development user secrets " +
                "or set the ConnectionStrings__SurplusLink environment variable before running EF tooling.");
        }
        var options = new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SurplusLinkDbContext(options);
    }
}
