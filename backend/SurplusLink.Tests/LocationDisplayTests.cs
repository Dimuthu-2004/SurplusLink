using System.Reflection;

namespace SurplusLink.Tests;

public sealed class LocationDisplayTests
{
    [Theory]
    [InlineData("42 Main Street, Negombo, Gampaha", "Negombo")]
    [InlineData("No. 10, Kandy Road, Kadawatha", "Kadawatha")]
    [InlineData("Colombo", "Colombo")]
    public void Concise_prefers_readable_locality(string address, string expected)
    {
        var type = typeof(SurplusLink.Api.Matching.MatchResponse).Assembly.GetType("SurplusLink.Api.Matching.LocationDisplay")!;
        var method = type.GetMethod("Concise", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(expected, method.Invoke(null, [address]));
    }
}
