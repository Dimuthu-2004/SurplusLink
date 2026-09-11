using SurplusLink.Api.Data;

namespace SurplusLink.Tests;

public sealed class ArchitecturePlaceholderTests
{
    [Fact]
    public void Api_persistence_boundary_is_available()
    {
        Assert.NotNull(typeof(SurplusLinkDbContext));
    }
}
