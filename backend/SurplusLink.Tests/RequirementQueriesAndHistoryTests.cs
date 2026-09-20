using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class RequirementQueriesAndHistoryTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Search_filters_and_totals_are_scoped_before_pagination()
    {
        var category = await Category("Search Category " + Guid.NewGuid());
        var otherCategory = await Category("Other " + Guid.NewGuid());
        var note = "Delivery 100%_ready\\loading " + Guid.NewGuid();
        var deadline = DateTime.UtcNow.Date.AddDays(3);
        var first = await Seed(category.Id, note, deadline, 100, BuyerRequestStatus.OPEN);
        await Seed(category.Id, "different", deadline.AddDays(1), 200, BuyerRequestStatus.DRAFT);
        await Seed(otherCategory.Id, note.ToUpperInvariant(), deadline, 300, BuyerRequestStatus.OPEN);
        await Seed(category.Id, note, deadline, 400, BuyerRequestStatus.OPEN, fixture.OtherBuyer);
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        var ownCategory = await Page(buyer, "/api/requirements/my?search=" + Uri.EscapeDataString(category.Name.ToLowerInvariant()));
        Assert.Equal(2, ownCategory.Total);
        Assert.All(ownCategory.Items, x => Assert.Equal(fixture.Buyer, x.BuyerId));
        var ownNotes = await Page(buyer, "/api/requirements/my?search=" + Uri.EscapeDataString(note));
        Assert.Equal(2, ownNotes.Total);
        var allNotes = await Page(manager, "/api/requirements?search=" + Uri.EscapeDataString(note));
        Assert.Equal(3, allNotes.Total);
        var instant = Uri.EscapeDataString(new DateTimeOffset(deadline).ToOffset(TimeSpan.FromHours(5.5)).ToString("O"));
        var combined = await Page(buyer, $"/api/requirements/my?search={Uri.EscapeDataString(note)}&status=open&categoryId={category.Id}&deadlineFrom={instant}&deadlineTo={instant}&pageSize=1");
        Assert.Equal(1, combined.Total);
        Assert.Equal(first.Id, Assert.Single(combined.Items).Id);
        var empty = await Page(buyer, $"/api/requirements/my?categoryId={category.Id}&status=COMPLETED");
        Assert.Equal(0, empty.Total);
        Assert.Empty(empty.Items);
        var managerPage = await Page(manager, $"/api/requirements?categoryId={category.Id}&pageSize=1&page=2");
        Assert.Equal(3, managerPage.Total);
        Assert.Single(managerPage.Items);
        var beyond = await Page(buyer, $"/api/requirements/my?categoryId={category.Id}&page=100");
        Assert.Equal(2, beyond.Total);
        Assert.Empty(beyond.Items);
    }

    [PostgresFact]
    public async Task Every_sort_direction_is_stable_across_pages()
    {
        var category = await Category("Sort " + Guid.NewGuid());
        var now = DateTime.UtcNow.Date;
        var rows = new[]
        {
            await Seed(category.Id, "", now.AddDays(3), 300, created: now.AddDays(-3)),
            await Seed(category.Id, "", now.AddDays(1), 100, created: now.AddDays(-1)),
            await Seed(category.Id, "", now.AddDays(2), 200, created: now.AddDays(-2)),
            await Seed(category.Id, "", now.AddDays(2), 200, created: now.AddDays(-2))
        };
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        foreach (var sort in new[] { "deadline", "budget", "createdAt" })
        foreach (var direction in new[] { "asc", "desc" })
        {
            Func<BuyerRequest, IComparable> key = sort switch
            {
                "deadline" => x => x.Deadline, "budget" => x => x.MaximumBudget, _ => x => x.CreatedAtUtc
            };
            var ordered = direction == "asc" ? rows.OrderBy(key) : rows.OrderByDescending(key);
            var expected = ordered.ThenBy(x => x.Id).Select(x => x.Id).ToArray();
            var received = new List<Guid>();
            for (var page = 1; page <= 2; page++)
            {
                var response = await Page(buyer, $"/api/requirements/my?categoryId={category.Id}&sort={sort}&sortDir={direction}&page={page}&pageSize=2");
                Assert.Equal(4, response.Total);
                Assert.Equal(page, response.Page);
                Assert.Equal(2, response.PageSize);
                received.AddRange(response.Items.Select(x => x.Id));
            }
            Assert.Equal(expected, received);
        }
    }

    [PostgresFact]
    public async Task Query_validation_and_optional_notes_contract_are_enforced()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        foreach (var (client, route) in new[] { (buyer, "/api/requirements/my"), (manager, "/api/requirements") })
        foreach (var query in new[]
        {
            "status=unknown", "status=1", "categoryId=invalid", "categoryId=" + Guid.Empty,
            "deadlineFrom=not-a-date", "deadlineFrom=2030-02-02&deadlineTo=2030-01-01",
            "sort=notes", "sortDir=random", "page=-1", "pageSize=101", "search=" + new string('x', 201)
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(route + "?" + query)).StatusCode);
        var request = Body("  Please deliver after lunch  ");
        var response = await buyer.PostAsJsonAsync("/api/requirements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var row = (await response.Content.ReadFromJsonAsync<RequirementResponse>())!;
        Assert.Equal("Please deliver after lunch", row.Notes);
        request["notes"] = new string('x', 2001);
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.PutAsJsonAsync($"/api/requirements/{row.Id}", request)).StatusCode);
        request["notes"] = null;
        var update = await buyer.PutAsJsonAsync($"/api/requirements/{row.Id}", request);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("", (await update.Content.ReadFromJsonAsync<RequirementResponse>())!.Notes);
    }

    [PostgresFact]
    public async Task History_records_operations_and_status_pairs_and_enforces_access()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var other = fixture.Client(app, fixture.OtherBuyer);
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var anonymous = app.CreateClient();
        var response = await buyer.PostAsJsonAsync("/api/requirements", Body());
        var row = (await response.Content.ReadFromJsonAsync<RequirementResponse>())!;
        var path = $"/api/requirements/{row.Id}";
        Assert.Equal(HttpStatusCode.OK, (await buyer.PutAsJsonAsync(path, Body("updated"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/submit", null)).StatusCode);
        var submitted = await History(buyer, path);
        Assert.Equal(4, submitted.Total); // created, updated, submitted, DRAFT -> OPEN
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/start-matching", null)).StatusCode);
        Assert.Equal(submitted.Total + 2, (await History(buyer, path)).Total);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/cancel", null)).StatusCode);
        var history = await History(buyer, path);
        Assert.Equal(6, history.Total);
        Assert.Equal(new[] { "CREATED", "UPDATED", "SUBMITTED", "MATCHING_STARTED" }.OrderBy(x => x),
            history.Items.Where(x => x.Action != "STATUS_CHANGED").Select(x => x.Action).OrderBy(x => x));
        var changes = history.Items.Where(x => x.Action == "STATUS_CHANGED").ToArray();
        Assert.Collection(changes, x => { Assert.Equal("DRAFT", x.FromStatus); Assert.Equal("OPEN", x.ToStatus); },
            x => { Assert.Equal("OPEN", x.FromStatus); Assert.Equal("MATCHING", x.ToStatus); });
        Assert.All(history.Items, x => Assert.Equal(fixture.Buyer, x.ActorUserId));
        Assert.Equal(history.Items.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => x.Id), history.Items.Select(x => x.Id));
        var page = (await buyer.GetFromJsonAsync<RequirementHistoryPage>(path + "/history?page=2&pageSize=2"))!;
        Assert.Equal(6, page.Total);
        Assert.Equal(history.Items.Skip(2).Take(2).Select(x => x.Id), page.Items.Select(x => x.Id));
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(path + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(path + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync(path + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync($"/api/requirements/{Guid.NewGuid()}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync(path + "/history?pageSize=0")).StatusCode);
    }

    [PostgresFact]
    public async Task Concurrent_start_and_future_tracked_status_changes_are_audited_atomically()
    {
        using var app = fixture.App(new SuccessfulStarter());
        using var buyer = fixture.Client(app, fixture.Buyer);
        var response = await buyer.PostAsJsonAsync("/api/requirements", Body());
        var row = (await response.Content.ReadFromJsonAsync<RequirementResponse>())!;
        var path = $"/api/requirements/{row.Id}";
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/submit", null)).StatusCode);
        var starts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => buyer.PostAsync(path + "/start-matching", null)));
        Assert.Single(starts, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(starts, x => x.StatusCode == HttpStatusCode.Conflict);
        var history = await History(buyer, path);
        Assert.Single(history.Items, x => x.Action == "MATCHING_STARTED");
        Assert.Single(history.Items, x => x.FromStatus == "OPEN" && x.ToStatus == "MATCHING");
        using (var db = fixture.Context())
        {
            var tracked = await db.BuyerRequests.SingleAsync(x => x.Id == row.Id);
            tracked.Status = BuyerRequestStatus.MATCH_FOUND;
            await db.SaveChangesAsync();
            await db.SaveChangesAsync(); // Unchanged state must not generate another event.
        }
        history = await History(buyer, path);
        var background = Assert.Single(history.Items, x => x.FromStatus == "MATCHING" && x.ToStatus == "MATCH_FOUND");
        Assert.Null(background.ActorUserId);
        using (var db = fixture.Context())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var tracked = await db.BuyerRequests.SingleAsync(x => x.Id == row.Id);
            tracked.Status = BuyerRequestStatus.PENDING_APPROVAL;
            db.SaveChanges(); // Also cover the synchronous SaveChanges overload.
            await tx.RollbackAsync();
        }
        Assert.Equal(history.Total, (await History(buyer, path)).Total);
        Assert.Equal(BuyerRequestStatus.MATCH_FOUND, (await buyer.GetFromJsonAsync<RequirementResponse>(path))!.Status);
    }

    [PostgresFact]
    public async Task Analytics_returns_consistent_counts_upcoming_preview_and_unit_aware_averages()
    {
        var category = await Category("Analytics " + Guid.NewGuid());
        foreach (var status in Enum.GetValues<BuyerRequestStatus>())
            await Seed(category.Id, "", DateTime.UtcNow.Date.AddDays(2), 100, status);
        for (var i = 0; i < 11; i++)
            await Seed(category.Id, "", DateTime.UtcNow.Date.AddDays(3), 200, BuyerRequestStatus.OPEN);
        await Seed(category.Id, "", DateTime.UtcNow.AddDays(-1), 300, BuyerRequestStatus.OPEN);
        await Seed(category.Id, "", DateTime.UtcNow.AddDays(30), 400, BuyerRequestStatus.OPEN);
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var anonymous = app.CreateClient();
        const string path = "/api/requirements/analytics/summary";
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync(path + "?upcomingDays=0")).StatusCode);
        var result = (await manager.GetFromJsonAsync<RequirementAnalyticsSummary>(path))!;
        using var db = fixture.Context();
        var all = await db.BuyerRequests.AsNoTracking().ToListAsync();
        Assert.Equal(all.Count, result.Total);
        Assert.Equal(9, result.CountsByStatus.Count);
        foreach (var group in result.CountsByStatus)
            Assert.Equal(all.Count(x => x.Status.ToString() == group.Status), group.Count);
        Assert.Equal(all.Count(x => x.Status == BuyerRequestStatus.OPEN), result.OpenCount);
        foreach (var group in result.CountsByCategory)
            Assert.Equal(all.Count(x => x.CategoryId == group.CategoryId), group.Count);
        Assert.Equal(7, (result.UpcomingUntil - result.AsOf).TotalDays);
        var inactive = new[] { BuyerRequestStatus.DRAFT, BuyerRequestStatus.REJECTED, BuyerRequestStatus.COMPLETED, BuyerRequestStatus.CANCELLED };
        var upcoming = all.Where(x => x.Deadline >= result.AsOf && x.Deadline <= result.UpcomingUntil && !inactive.Contains(x.Status))
            .OrderBy(x => x.Deadline).ThenBy(x => x.Id).ToArray();
        Assert.Equal(upcoming.Length, result.UpcomingDeadlineCount);
        Assert.Equal(upcoming.Take(10).Select(x => x.Id), result.UpcomingDeadlines.Select(x => x.Id));
        Assert.NotNull(result.AverageMaximumBudget);
        // PostgreSQL AVG and .NET decimal division retain different trailing precision.
        Assert.InRange(Math.Abs(all.Average(x => x.MaximumBudget) - result.AverageMaximumBudget.Value), 0, 0.000000000001m);
        foreach (var group in result.AverageQuantityByUnit)
        {
            Assert.InRange(Math.Abs(all.Where(x => x.Unit == group.Unit).Average(x => x.RequiredQuantity) -
                group.AverageRequiredQuantity), 0, 0.000000000001m);
            Assert.Equal(all.Count(x => x.Unit == group.Unit), group.Count);
        }
        using var document = JsonDocument.Parse(await manager.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty(path, out _));
        Assert.True(paths.TryGetProperty("/api/requirements/{id}/history", out _));
        var parameters = paths.GetProperty("/api/requirements/my").GetProperty("get").GetProperty("parameters")
            .EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();
        foreach (var name in new[] { "Search", "Status", "CategoryId", "DeadlineFrom", "DeadlineTo", "Sort", "SortDir", "Page", "PageSize" })
            Assert.Contains(parameters, x => string.Equals(name, x, StringComparison.OrdinalIgnoreCase));
    }

    [PostgresFact]
    public async Task Empty_analytics_has_zero_counts_and_null_budget_average()
    {
        // A separate disposable fixture keeps the empty-data case independent of other tests.
        var empty = new RequirementsDatabase();
        await empty.InitializeAsync();
        try
        {
            using var db = empty.Context();
            await db.Reservations.ExecuteDeleteAsync();
            await db.Matches.ExecuteDeleteAsync();
            await db.BuyerRequests.ExecuteDeleteAsync();
            using var app = empty.App();
            using var manager = empty.Client(app, empty.Manager, "MANAGER");
            var result = (await manager.GetFromJsonAsync<RequirementAnalyticsSummary>("/api/requirements/analytics/summary"))!;
            Assert.Equal(0, result.Total);
            Assert.Equal(0, result.OpenCount);
            Assert.Equal(0, result.UpcomingDeadlineCount);
            Assert.All(result.CountsByStatus, x => Assert.Equal(0, x.Count));
            Assert.All(result.CountsByCategory, x => Assert.Equal(0, x.Count));
            Assert.Empty(result.UpcomingDeadlines);
            Assert.Empty(result.AverageQuantityByUnit);
            Assert.Null(result.AverageMaximumBudget);
        }
        finally { await empty.DisposeAsync(); }
    }

    private async Task<Category> Category(string name)
    {
        using var db = fixture.Context();
        var category = new Category { Id = Guid.NewGuid(), Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private async Task<BuyerRequest> Seed(Guid category, string notes, DateTime deadline, decimal budget,
        BuyerRequestStatus status = BuyerRequestStatus.DRAFT, Guid? buyer = null, DateTime? created = null)
    {
        using var db = fixture.Context();
        var row = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = buyer ?? fixture.Buyer, CategoryId = category,
            Title = "Query test", Notes = notes, RequiredQuantity = budget / 10, Unit = "kg",
            MaximumBudget = budget, Deadline = deadline, Status = status, Latitude = 6, Longitude = 79,
            CreatedAtUtc = created ?? DateTime.UtcNow
        };
        db.BuyerRequests.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    private static Dictionary<string, object?> Body(string? notes = null) => new()
    {
        ["categoryId"] = Guid.Parse("00000000-0000-0000-0000-000000000101"), ["notes"] = notes,
        ["requiredQuantity"] = 10, ["unit"] = "kg", ["maximumBudget"] = 25000,
        ["deadline"] = DateTimeOffset.UtcNow.AddDays(7), ["latitude"] = 6.9271m, ["longitude"] = 79.8612m
    };
    private static async Task<RequirementPage> Page(HttpClient client, string path) =>
        (await client.GetFromJsonAsync<RequirementPage>(path))!;
    private static async Task<RequirementHistoryPage> History(HttpClient client, string path) =>
        (await client.GetFromJsonAsync<RequirementHistoryPage>(path + "/history"))!;
    private sealed class SuccessfulStarter : IRequirementWorkflowStarter
    {
        public Task<Guid> StartAsync(Guid requirementId, Guid buyerId, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid()); // Test-only seam; canonical workflow remains M4-owned.
    }
}
