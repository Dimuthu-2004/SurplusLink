using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MatchIntegrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Filters_totals_sorting_and_pagination_are_requirement_scoped()
    {
        var (request, listing) = await Seed();
        using var db = fixture.Context();
        var service = new MatchService(db);
        var first = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        await service.RankAsync(first.Id, .8m, fixture.Manager, default);
        await service.RecordRouteAsync(first.Id, true, 10, 100, fixture.Manager, default);
        var (_, secondListing) = await Seed();
        var second = await service.GenerateAsync(request.Id, secondListing.Id, fixture.Manager, default);
        await service.RankAsync(second.Id, .2m, fixture.Manager, default);
        await service.RejectAsync(second.Id, "TOO_EXPENSIVE", fixture.Manager, default);
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer, "SELLER", "BUYER");
        var path = $"/api/matches/requirement/{request.Id}";
        var page = (await buyer.GetFromJsonAsync<MatchPage>(path + "?pageSize=1"))!;
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(first.Id, Assert.Single(page.Items).Id);
        Assert.Equal(second.Id, Assert.Single((await buyer.GetFromJsonAsync<MatchPage>(path + "?pageSize=1&page=2"))!.Items).Id);
        Assert.Empty((await buyer.GetFromJsonAsync<MatchPage>(path + "?page=99"))!.Items);
        foreach (var query in new[] { "valid=true", "rejected=false", "status=routed", "valid=true&status=ROUTED" })
        {
            var filtered = (await buyer.GetFromJsonAsync<MatchPage>(path + "?" + query))!;
            Assert.Equal(1, filtered.Total);
            Assert.Equal(first.Id, Assert.Single(filtered.Items).Id);
        }
        Assert.Equal(second.Id, Assert.Single((await buyer.GetFromJsonAsync<MatchPage>(path + "?rejected=true"))!.Items).Id);
        Assert.Empty((await buyer.GetFromJsonAsync<MatchPage>(path + "?valid=true&rejected=true"))!.Items);
        foreach (var sort in new[] { "score", "distance", "estimatedTransportCost", "createdAt" })
        foreach (var direction in new[] { "asc", "desc" })
        {
            var sorted = (await buyer.GetFromJsonAsync<MatchPage>(path + $"?sortBy={sort}&sortDir={direction}"))!;
            Assert.Equal(2, sorted.Total);
            var expected = MatchQueryBuilder.Sort(new[] { first, second }.AsQueryable(), new() { SortBy = sort, SortDir = direction });
            Assert.Equal(expected.Select(x => x.Id), sorted.Items.Select(x => x.Id));
        }
        foreach (var query in new[] { "valid=maybe", "status=1", "status=unknown", "sortBy=bad", "sortDir=up", "page=0", "pageSize=101" })
            Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync(path + "?" + query)).StatusCode);
    }

    [PostgresFact]
    public async Task History_and_analytics_track_actions_and_route_attempts_with_access_control()
    {
        var (request, listing) = await Seed();
        using var db = fixture.Context();
        var service = new MatchService(db);
        var before = await service.SummaryAsync(default);
        var match = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        await service.RankAsync(match.Id, .75m, fixture.Manager, default);
        await service.RecordRouteAsync(match.Id, false, null, null, fixture.Manager, default);
        await service.RecordRouteAsync(match.Id, true, 25, 500, fixture.Manager, default);
        await service.RejectAsync(match.Id, "  TRANSPORT_OVER_BUDGET  ", fixture.Manager, default);
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var other = fixture.Client(app, fixture.OtherBuyer);
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var guest = app.CreateClient();
        var historyPath = $"/api/matches/{match.Id}/history";
        var history = (await buyer.GetFromJsonAsync<MatchHistoryPage>(historyPath))!;
        Assert.Equal(5, history.Total);
        Assert.Equal(new[] { "GENERATE", "RANK", "ROUTE", "ROUTE", "REJECT" }, history.Items.Select(x => x.Action));
        Assert.Equal(new[] { "FAILED", "SUCCEEDED" }, history.Items.Where(x => x.Action == "ROUTE").Select(x => x.Outcome));
        Assert.All(history.Items, x => Assert.Equal(fixture.Manager, x.ActorUserId));
        var page = (await buyer.GetFromJsonAsync<MatchHistoryPage>(historyPath + "?page=2&pageSize=2"))!;
        Assert.Equal(5, page.Total);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(history.Items.Skip(2).Take(2).Select(x => x.Id), page.Items.Select(x => x.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync(historyPath + "?pageSize=0")).StatusCode);
        foreach (var path in new[] { historyPath, $"/api/matches/requirement/{request.Id}" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(path)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync($"/api/matches/{Guid.NewGuid()}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync($"/api/matches/requirement/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/matches/analytics/summary")).StatusCode);
        var summary = (await manager.GetFromJsonAsync<MatchAnalyticsSummary>("/api/matches/analytics/summary"))!;
        Assert.Equal(before.RouteSuccessCount + 1, summary.RouteSuccessCount);
        Assert.Equal(before.RouteFailureCount + 1, summary.RouteFailureCount);
        Assert.Equal(1d, summary.RouteSuccessRate!.Value + summary.RouteFailureRate!.Value, 10);
        Assert.Contains(summary.TopRejectionReasons, x => x.Reason == "TRANSPORT_OVER_BUDGET" && x.Count == 1);
        Assert.Equal(await db.Matches.AverageAsync(x => (decimal?)x.Score), summary.AverageScore);
        Assert.Equal(await db.Matches.AverageAsync(x => x.Distance), summary.AverageDistance);
        var logs = await db.AuditLogs.CountAsync(x => x.EntityId == match.Id);
        Assert.Equal(409, (await Assert.ThrowsAsync<MatchException>(() => service.RankAsync(match.Id, .5m, fixture.Manager, default))).StatusCode);
        Assert.Equal(logs, await db.AuditLogs.CountAsync(x => x.EntityId == match.Id));
    }

    [PostgresFact]
    public async Task Self_match_is_rejected_and_failed_writes_leave_no_audit_entries()
    {
        var (request, listing) = await Seed(selfOwned: true);
        using var db = fixture.Context();
        var service = new MatchService(db);
        var match = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        Assert.Equal(MatchStatus.REJECTED, match.Status);
        Assert.Equal(MarketplaceMatchPolicy.SelfMatchNotAllowed, match.RejectionReason);
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.EntityId == match.Id));
        Assert.Equal(409, (await Assert.ThrowsAsync<MatchException>(() => service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<MatchException>(() => service.RecordRouteAsync(match.Id, true, -1, 0, fixture.Manager, default))).StatusCode);
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.EntityId == match.Id));
    }

    [PostgresFact]
    public async Task Concurrent_rejection_prevents_stale_rank_and_rolls_back_its_audit()
    {
        var (request, listing) = await Seed();
        using var first = fixture.Context();
        var service = new MatchService(first);
        var match = await service.GenerateAsync(request.Id, listing.Id, fixture.Manager, default);
        using var stale = fixture.Context();
        await stale.Matches.SingleAsync(x => x.Id == match.Id);
        await service.RejectAsync(match.Id, "NO_LONGER_NEEDED", fixture.Manager, default);
        var error = await Assert.ThrowsAsync<MatchException>(() =>
            new MatchService(stale).RankAsync(match.Id, .9m, fixture.Manager, default));
        Assert.Equal(409, error.StatusCode);
        using var verify = fixture.Context();
        Assert.Equal(MatchStatus.REJECTED, (await verify.Matches.SingleAsync(x => x.Id == match.Id)).Status);
        Assert.Equal(new[] { "GENERATE", "REJECT" }, await verify.AuditLogs.Where(x => x.EntityId == match.Id)
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Select(x => x.Action).ToArrayAsync());
    }

    [PostgresFact]
    public async Task Empty_analytics_and_legacy_matches_do_not_invent_routes_or_history()
    {
        var empty = new RequirementsDatabase();
        await empty.InitializeAsync();
        try
        {
            using var db = empty.Context();
            var service = new MatchService(db);
            var legacy = await db.Matches.SingleAsync();
            Assert.Equal(MatchStatus.GENERATED, legacy.Status);
            Assert.Null(legacy.Distance);
            Assert.Null(legacy.EstimatedTransportCost);
            Assert.Empty((await service.HistoryAsync(legacy.Id, empty.Buyer, false, new(), default)).Items);
            var withLegacy = await service.SummaryAsync(default);
            Assert.Equal(.5m, withLegacy.AverageScore);
            Assert.Null(withLegacy.AverageDistance);
            Assert.Null(withLegacy.RouteSuccessRate);
            Assert.Null(withLegacy.RouteFailureRate);
            await db.Matches.ExecuteDeleteAsync();
            var summary = await service.SummaryAsync(default);
            Assert.Equal(0, summary.Total);
            Assert.Null(summary.AverageScore);
            Assert.Null(summary.AverageDistance);
            Assert.Empty(summary.TopRejectionReasons);
            Assert.Equal(0, summary.RouteSuccessCount);
            Assert.Equal(0, summary.RouteFailureCount);
            var page = await service.ListAsync(empty.LegacyRequest, empty.Buyer, false, new(), default);
            Assert.Equal(0, page.Total);
            Assert.Equal(0, page.TotalPages);
            Assert.Empty(page.Items);
        }
        finally { await empty.DisposeAsync(); }
    }

    private async Task<(BuyerRequest Request, Listing Listing)> Seed(bool selfOwned = false)
    {
        using var db = fixture.Context();
        var category = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category, RequiredQuantity = 5,
            MaximumBudget = 10000, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(7), Status = BuyerRequestStatus.OPEN
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = selfOwned ? fixture.Buyer : fixture.Seller, CategoryId = category,
            Title = "Matching stock", Quantity = 100, Unit = "kg", UnitPrice = 10,
            AvailableUntil = DateTime.UtcNow.AddDays(10), Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        db.AddRange(request, listing);
        await db.SaveChangesAsync();
        return (request, listing);
    }
}
