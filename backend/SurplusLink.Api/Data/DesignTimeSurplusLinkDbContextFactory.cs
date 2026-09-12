using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SurplusLink.Api.Data;

public sealed class DesignTimeSurplusLinkDbContextFactory : IDesignTimeDbContextFactory<SurplusLinkDbContext>
{
    public SurplusLinkDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SurplusLink")
            ?? "Host=localhost;Database=surpluslink";
        var options = new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SurplusLinkDbContext(options);
    }
}
