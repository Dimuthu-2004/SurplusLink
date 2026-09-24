using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MatchQueryTests
{
    [Theory]
    [InlineData("score", "asc")]
    [InlineData("score", "desc")]
    [InlineData("distance", "asc")]
    [InlineData("distance", "desc")]
    [InlineData("estimatedTransportCost", "asc")]
    [InlineData("estimatedTransportCost", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    public void Sorts_have_stable_ties_and_unknown_routes_last(string sort, string direction)
    {
        var rows = Enumerable.Range(1, 4).Select(i => new MaterialMatch
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{i:000000000000}"),
            Score = i == 4 ? 0.2m : i / 10m,
            Distance = i == 1 ? null : i == 4 ? 2 : i,
            EstimatedTransportCost = i == 1 ? null : i == 4 ? 2 : i,
            CreatedAtUtc = DateTime.UnixEpoch.AddDays(i == 4 ? 2 : i)
        }).Reverse().ToArray();
        var query = new MatchQuery { SortBy = sort, SortDir = direction };
        var result = MatchQueryBuilder.Sort(rows.AsQueryable(), query).ToArray();
        Func<MaterialMatch, decimal?> key = sort switch
        {
            "score" => x => x.Score, "distance" => x => x.Distance,
            "estimatedTransportCost" => x => x.EstimatedTransportCost,
            _ => x => x.CreatedAtUtc.Ticks
        };
        var expected = rows.OrderBy(x => key(x) is null);
        expected = direction == "asc" ? expected.ThenBy(key) : expected.ThenByDescending(key);
        Assert.Equal(expected.ThenBy(x => x.Id).Select(x => x.Id), result.Select(x => x.Id));
        using var db = new SurplusLinkDbContext(new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        // Exercise PostgreSQL translation without connecting to a database.
        Assert.Contains("ORDER BY", MatchQueryBuilder.Sort(db.Matches, query).ToQueryString());
    }

    [Fact]
    public void Filters_compose_and_conflicting_filters_return_no_matches()
    {
        var rows = Enum.GetValues<MatchStatus>().Select(status => new MaterialMatch { Status = status }).AsQueryable();
        Assert.Equal(1, MatchQueryBuilder.Filter(rows, new() { Valid = true }).Count());
        Assert.Single(MatchQueryBuilder.Filter(rows, new() { Rejected = true }));
        Assert.Single(MatchQueryBuilder.Filter(rows, new() { Valid = false, Rejected = true, Status = "rejected" }));
        Assert.Empty(MatchQueryBuilder.Filter(rows, new() { Valid = true, Rejected = true }));
        Assert.Single(MatchQueryBuilder.Filter(rows, new() { Valid = true, Status = "routed" }));
    }

    [Fact]
    public void Query_validation_rejects_unknown_status_sort_direction_and_unsafe_paging()
    {
        foreach (var query in new MatchQuery[]
        {
            new() { Status = "1" }, new() { Status = "unknown" }, new() { SortBy = "cost" },
            new() { SortDir = "up" }, new() { Page = 0 }, new() { Page = int.MaxValue },
            new() { PageSize = 0 }, new() { PageSize = 101 }
        })
            Assert.False(Validator.TryValidateObject(query, new ValidationContext(query), [], true));
        var valid = new MatchQuery { Status = "route_failed", SortBy = "DISTANCE", SortDir = "ASC" };
        Assert.True(Validator.TryValidateObject(valid, new ValidationContext(valid), [], true));
    }
}
