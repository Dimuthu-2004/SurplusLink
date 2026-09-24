using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Data;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SURPLUSLINK_TEST_CONNECTION")))
            Skip = "Set SURPLUSLINK_TEST_CONNECTION to PostgreSQL credentials with permission to create a disposable test database.";
    }
}

public sealed class BuyerRequirementsIntegrationTests : IClassFixture<RequirementsDatabase>
{
    private readonly RequirementsDatabase fixture;
    public BuyerRequirementsIntegrationTests(RequirementsDatabase fixture) => this.fixture = fixture;

    [PostgresFact]
    public async Task Draft_crud_preserves_owner_timestamps_and_lists_only_own_rows()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var other = fixture.Client(app, fixture.OtherBuyer);
        var created = await Create(buyer);
        Assert.Equal(fixture.Buyer, created.BuyerId);
        Assert.Equal(BuyerRequestStatus.DRAFT, created.Status);
        Assert.Equal("kg", created.Unit);
        Assert.True(created.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
        var update = await buyer.PutAsJsonAsync($"/api/requirements/{created.Id}", Body(quantity: 12));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var edited = (await update.Content.ReadFromJsonAsync<RequirementResponse>())!;
        Assert.Equal(12, edited.RequiredQuantity);
        // PostgreSQL timestamps retain microseconds; .NET timestamps also contain sub-microsecond ticks.
        Assert.Equal(created.CreatedAt, edited.CreatedAt, TimeSpan.FromMicroseconds(1));
        Assert.True(edited.UpdatedAt >= created.UpdatedAt);
        var mine = (await buyer.GetFromJsonAsync<RequirementPage>("/api/requirements/my"))!;
        Assert.Contains(mine.Items, x => x.Id == created.Id);
        Assert.All(mine.Items, x => Assert.Equal(fixture.Buyer, x.BuyerId));
        var theirs = (await other.GetFromJsonAsync<RequirementPage>("/api/requirements/my"))!;
        Assert.DoesNotContain(theirs.Items, x => x.Id == created.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await buyer.DeleteAsync($"/api/requirements/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync($"/api/requirements/{created.Id}")).StatusCode);
    }

    [PostgresFact]
    public async Task Authentication_ownership_and_manager_read_only_access_cover_every_route()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var other = fixture.Client(app, fixture.OtherBuyer);
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var anonymous = app.CreateClient();
        var row = await Create(buyer);
        var path = $"/api/requirements/{row.Id}";
        foreach (var client in new[] { other, seller, anonymous })
        {
            var expected = client == anonymous ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync(path, Body())).StatusCode);
            Assert.Equal(expected, (await client.DeleteAsync(path)).StatusCode);
            foreach (var action in new[] { "submit", "start-matching", "cancel" })
                Assert.Equal(expected, (await client.PostAsync(path + "/" + action, null)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/requirements")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/requirements")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync("/api/requirements", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(path, Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.DeleteAsync(path)).StatusCode);
        foreach (var action in new[] { "submit", "start-matching", "cancel" })
            Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync(path + "/" + action, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/requirements/my")).StatusCode);
    }

    [PostgresFact]
    public async Task Marketplace_role_matrix_preserves_listing_ownership_admin_boundaries_and_self_match_exclusion()
    {
        using var app = fixture.App();
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");
        using var dual = fixture.Client(app, fixture.Buyer, "SELLER", "BUYER");

        var sellerListingResponse = await seller.PostAsJsonAsync("/api/materials", ListingBody());
        Assert.Equal(HttpStatusCode.Created, sellerListingResponse.StatusCode);
        var sellerListing = (await sellerListingResponse.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
        Assert.Equal(fixture.Seller, sellerListing.SellerId);

        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync("/api/materials", ListingBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await buyer.PutAsJsonAsync($"/api/materials/{sellerListing.Id}", ListingBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.DeleteAsync($"/api/materials/{sellerListing.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PatchAsync($"/api/materials/{sellerListing.Id}/publish", null)).StatusCode);

        var dualListingResponse = await dual.PostAsJsonAsync("/api/materials", ListingBody());
        Assert.Equal(HttpStatusCode.Created, dualListingResponse.StatusCode);
        var dualListing = (await dualListingResponse.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
        Assert.Equal(fixture.Buyer, dualListing.SellerId);
        Assert.Equal(HttpStatusCode.OK,
            (await dual.PutAsJsonAsync($"/api/materials/{dualListing.Id}", ListingBody())).StatusCode);
        var ownListings = (await dual.GetFromJsonAsync<PagedMaterialListingsResponse>("/api/materials?mineOnly=true"))!;
        Assert.Contains(ownListings.Items, item => item.Id == dualListing.Id && item.SellerId == fixture.Buyer);
        Assert.Equal(HttpStatusCode.NotFound,
            (await dual.PutAsJsonAsync($"/api/materials/{sellerListing.Id}", ListingBody())).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await dual.PatchAsync($"/api/materials/{dualListing.Id}/publish", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await dual.PatchAsJsonAsync($"/api/materials/{dualListing.Id}/verify", new { approved = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await dual.PostAsJsonAsync("/api/material-categories", new { name = "dual-forbidden-" + Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await dual.PutAsJsonAsync($"/api/material-categories/{Guid.NewGuid()}", new { name = "dual-forbidden" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await dual.DeleteAsync($"/api/material-categories/{Guid.NewGuid()}")).StatusCode);

        var requirementResponse = await Create(dual);
        using var db = fixture.Context();
        var requirement = await db.BuyerRequests.SingleAsync(x => x.Id == requirementResponse.Id);
        requirement.Status = BuyerRequestStatus.MATCHING;
        var selfOwnedListing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Buyer, CategoryId = requirement.CategoryId,
            Title = "Self-owned stock", Quantity = 100, Unit = requirement.Unit, UnitPrice = 1,
            AvailableUntil = requirement.Deadline.AddDays(1), Status = ListingStatus.ACTIVE,
            Condition = MaterialCondition.GOOD
        };
        db.Listings.Add(selfOwnedListing);
        await db.SaveChangesAsync();
        Assert.Equal(MarketplaceMatchPolicy.SelfMatchNotAllowed,
            MarketplaceMatchPolicy.RejectionReason(requirement.BuyerId, selfOwnedListing.SellerId));
    }

    [PostgresFact]
    public async Task Invalid_values_unknown_fields_and_missing_ids_are_rejected()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        var invalid = new (string Key, object? Value)[]
        {
            ("categoryId", Guid.Empty), ("categoryId", Guid.NewGuid()),
            ("requiredQuantity", 0), ("requiredQuantity", -1), ("requiredQuantity", 0.0001m),
            ("maximumBudget", 0), ("maximumBudget", -1), ("maximumBudget", 0.001m),
            ("unit", "   "), ("unit", new string('x', 33)),
            ("deadline", DateTimeOffset.UtcNow.AddDays(-1)), ("deadline", null),
            ("latitude", 91), ("longitude", -181), ("latitude", null), ("longitude", null),
            ("buyerId", fixture.OtherBuyer), ("status", "APPROVED")
        };
        foreach (var (key, value) in invalid)
        {
            var body = Body(); body[key] = value;
            var result = await buyer.PostAsJsonAsync("/api/requirements", body);
            Assert.True(result.StatusCode == HttpStatusCode.BadRequest, key + ": " + await result.Content.ReadAsStringAsync());
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync("/api/requirements/my?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync("/api/requirements/my?pageSize=101")).StatusCode);
        var missing = $"/api/requirements/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync(missing)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.PutAsJsonAsync(missing, Body())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.DeleteAsync(missing)).StatusCode);
        foreach (var action in new[] { "submit", "start-matching", "cancel" })
            Assert.Equal(HttpStatusCode.NotFound, (await buyer.PostAsync(missing + "/" + action, null)).StatusCode);
    }

    [PostgresFact]
    public async Task Extra_coordinate_precision_is_silently_rounded_to_six_decimals()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        var body = Body();
        body["latitude"] = 6.12345678m;   // 8 decimal places
        body["longitude"] = 79.98765432m; // 8 decimal places
        var response = await buyer.PostAsJsonAsync("/api/requirements", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<RequirementResponse>())!;
        Assert.Equal(6.123457m, created.Latitude);   // rounded to 6
        Assert.Equal(79.987654m, created.Longitude); // rounded to 6
    }

    [PostgresFact]
    public async Task Lifecycle_guards_and_persisted_workflow_leave_stock_unreserved()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        var row = await Create(buyer);
        var path = $"/api/requirements/{row.Id}";
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/start-matching", null)).StatusCode);
        var submitted = await buyer.PostAsync(path + "/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        Assert.Equal(BuyerRequestStatus.OPEN, (await submitted.Content.ReadFromJsonAsync<RequirementResponse>())!.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/submit", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PutAsJsonAsync(path, Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.DeleteAsync(path)).StatusCode);
        using var db = fixture.Context();
        var before = await Snapshot(db);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync(path + "/start-matching", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/start-matching", null)).StatusCode);
        var after = await Snapshot(db);
        Assert.Equal(before.Reservations, after.Reservations);
        Assert.Equal(before.Reserved, after.Reserved);
        Assert.Equal(before.Workflows + 1, after.Workflows);
        Assert.Equal(BuyerRequestStatus.MATCHING, (await buyer.GetFromJsonAsync<RequirementResponse>(path))!.Status);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == row.Id && x.Action == "MATCHING_STARTED"));
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/cancel", null)).StatusCode);
        foreach (var action in new[] { "submit", "start-matching", "cancel" })
            Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/" + action, null)).StatusCode);
        var draft = await Create(buyer);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync($"/api/requirements/{draft.Id}/cancel", null)).StatusCode);
    }

    [PostgresFact]
    public async Task Concurrent_submit_and_start_calls_transition_once_without_reserving()
    {
        var starter = new RecordingStarter();
        using var app = fixture.App(starter);
        using var buyer = fixture.Client(app, fixture.Buyer);
        var row = await Create(buyer);
        var path = $"/api/requirements/{row.Id}";
        var submits = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => buyer.PostAsync(path + "/submit", null)));
        Assert.Single(submits, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(submits, x => x.StatusCode == HttpStatusCode.Conflict);
        using var db = fixture.Context();
        var before = await Snapshot(db);
        var starts = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => buyer.PostAsync(path + "/start-matching", null)));
        var success = Assert.Single(starts, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Equal(2, starts.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        var result = (await success.Content.ReadFromJsonAsync<StartMatchingResponse>())!;
        Assert.Equal(starter.Id, result.WorkflowId);
        Assert.Equal(BuyerRequestStatus.MATCHING, result.Requirement.Status);
        Assert.Equal(1, starter.Calls);
        Assert.Equal(before, await Snapshot(db));
        Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/cancel", null)).StatusCode);
    }

    [PostgresFact]
    public async Task Expired_drafts_cannot_submit_and_expired_open_requests_cannot_start()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        foreach (var state in new[] { BuyerRequestStatus.DRAFT, BuyerRequestStatus.OPEN })
        {
            var row = await Create(buyer);
            using var db = fixture.Context();
            await db.BuyerRequests.Where(x => x.Id == row.Id).ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Deadline, DateTime.UtcNow.AddDays(-1)).SetProperty(x => x.Status, state));
            var action = state == BuyerRequestStatus.DRAFT ? "submit" : "start-matching";
            Assert.Equal(HttpStatusCode.BadRequest, (await buyer.PostAsync($"/api/requirements/{row.Id}/{action}", null)).StatusCode);
            Assert.Equal(state, (await db.BuyerRequests.AsNoTracking().SingleAsync(x => x.Id == row.Id)).Status);
        }
    }

    [PostgresFact]
    public async Task Database_constraints_reject_invalid_direct_writes_and_migration_preserves_legacy_links()
    {
        using var db = fixture.Context();
        var legacy = await db.BuyerRequests.SingleAsync(x => x.Id == fixture.LegacyRequest);
        Assert.Equal(BuyerRequestStatus.MATCH_FOUND, legacy.Status);
        Assert.Equal("unit", legacy.Unit);
        Assert.Equal("", legacy.Notes);
        Assert.Null(legacy.Latitude);
        Assert.Equal(5, legacy.RequiredQuantity);
        Assert.True(await db.Matches.AnyAsync(x => x.MaterialRequestId == legacy.Id));
        Assert.True(await db.Reservations.AnyAsync(x => x.MaterialRequestId == legacy.Id));
        foreach (var assignment in new[]
        {
            "\"Quantity\" = 0", "\"Budget\" = -1", "\"Unit\" = ' '", "\"Status\" = 'INVALID'",
            "\"Latitude\" = 91, \"Longitude\" = 0", "\"Latitude\" = 1, \"Longitude\" = NULL",
            "\"BuyerId\" = '00000000-0000-0000-0000-000000000999'",
            "\"CategoryId\" = '00000000-0000-0000-0000-000000000999'"
        })
        {
            // Static, test-owned assignments only; no user input enters SQL.
            var sql = "UPDATE \"MaterialRequests\" SET " + assignment + " WHERE \"Id\" = {0}";
            var error = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlRawAsync(sql, legacy.Id));
            Assert.Contains(error.SqlState, new[] { PostgresErrorCodes.CheckViolation, PostgresErrorCodes.ForeignKeyViolation });
        }
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains(db.Model.GetEntityTypes(), x => x.ClrType.Name == "AgentWorkflow");
        Assert.Contains(db.Model.GetEntityTypes(), x => x.ClrType.Name == "AgentStep");
        Assert.Contains(db.Model.GetEntityTypes(), x => x.ClrType.Name == "AgentToolCall");
        Assert.Contains(db.Model.GetEntityTypes(), x => x.ClrType.Name == "Approval");
    }

    [PostgresFact]
    public async Task Swagger_documents_all_requirement_routes_and_shared_auth_routes()
    {
        using var app = fixture.App();
        using var client = app.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var path in new[] { "/api/requirements", "/api/requirements/{id}", "/api/requirements/my",
            "/api/requirements/{id}/submit", "/api/requirements/{id}/start-matching", "/api/requirements/{id}/cancel" })
            Assert.True(paths.TryGetProperty(path, out _), path);
        Assert.Contains(paths.EnumerateObject(), x => x.Name.StartsWith("/api/auth"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [PostgresFact]
    public async Task Terminal_states_are_locked_while_match_found_requirements_can_be_edited_or_rerun()
    {
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        var states = Enum.GetValues<BuyerRequestStatus>().Except([
            BuyerRequestStatus.DRAFT, BuyerRequestStatus.OPEN, BuyerRequestStatus.MATCH_FOUND]);
        foreach (var state in states)
        {
            var row = await Create(buyer);
            using var db = fixture.Context();
            await db.BuyerRequests.Where(x => x.Id == row.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, state));
            var path = $"/api/requirements/{row.Id}";
            Assert.Equal(HttpStatusCode.Conflict, (await buyer.PutAsJsonAsync(path, Body())).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await buyer.DeleteAsync(path)).StatusCode);
            foreach (var action in new[] { "submit", "start-matching", "cancel" })
                Assert.Equal(HttpStatusCode.Conflict, (await buyer.PostAsync(path + "/" + action, null)).StatusCode);
            Assert.Equal(state, (await buyer.GetFromJsonAsync<RequirementResponse>(path))!.Status);
        }

        var matchFound = await Create(buyer);
        using (var db = fixture.Context())
            await db.BuyerRequests.Where(x => x.Id == matchFound.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, BuyerRequestStatus.MATCH_FOUND));
        var matchFoundPath = $"/api/requirements/{matchFound.Id}";
        Assert.Equal(HttpStatusCode.OK, (await buyer.PutAsJsonAsync(matchFoundPath, Body())).StatusCode);
        Assert.Equal(BuyerRequestStatus.OPEN,
            (await buyer.GetFromJsonAsync<RequirementResponse>(matchFoundPath))!.Status);

        using (var db = fixture.Context())
            await db.BuyerRequests.Where(x => x.Id == matchFound.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, BuyerRequestStatus.MATCH_FOUND));
        Assert.Equal(HttpStatusCode.OK,
            (await buyer.PostAsync(matchFoundPath + "/start-matching", null)).StatusCode);
    }

    private static async Task<(int Reservations, int Workflows, decimal Reserved)> Snapshot(SurplusLinkDbContext db) =>
        (await db.Reservations.CountAsync(),
            await db.Workflows.CountAsync() + await db.AgentWorkflows.CountAsync(),
            await db.Listings.SumAsync(x => x.ReservedQuantity));

    private static Dictionary<string, object?> Body(decimal quantity = 10) => new()
    {
        ["categoryId"] = Guid.Parse("00000000-0000-0000-0000-000000000101"),
        ["requiredQuantity"] = quantity, ["unit"] = " kg ", ["maximumBudget"] = 25000m,
        ["deadline"] = DateTimeOffset.UtcNow.AddDays(7), ["latitude"] = 6.9271m, ["longitude"] = 79.8612m
    };

    private static object ListingBody() => new
    {
        categoryId = "00000000-0000-0000-0000-000000000101",
        title = "Role matrix listing", description = "Test stock", quantity = 10,
        unit = "kg", condition = "GOOD", unitPrice = 5,
        availableUntil = DateTime.UtcNow.AddDays(30)
    };

    private static async Task<RequirementResponse> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/requirements", Body());
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<RequirementResponse>())!;
    }

    private sealed class RecordingStarter : IRequirementWorkflowStarter
    {
        public Guid Id { get; } = Guid.NewGuid(); // Test double only: no production workflow persistence claimed.
        public int Calls;
        public async Task<Guid> StartAsync(Guid requirementId, Guid buyerId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(100, cancellationToken);
            return Id;
        }
    }
}

public sealed class RequirementsDatabase : IAsyncLifetime
{
    private const string Secret = "buyer-requirements-tests-only-secret-at-least-32-characters";
    private readonly string databaseName = "surpluslink_m2_test_" + Guid.NewGuid().ToString("N");
    private string? adminConnection;
    private string? connection;
    private bool created;
    public Guid Buyer { get; } = Guid.NewGuid();
    public Guid OtherBuyer { get; } = Guid.NewGuid();
    public Guid Manager { get; } = Guid.NewGuid();
    public Guid Seller { get; } = Guid.NewGuid();
    public Guid LegacyRequest { get; } = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("SURPLUSLINK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured)) return;
        var builder = new NpgsqlConnectionStringBuilder(configured) { Database = "postgres", Pooling = false };
        adminConnection = builder.ConnectionString;
        builder.Database = databaseName;
        connection = builder.ConnectionString;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await command.ExecuteNonQueryAsync();
        created = true;
        try
        {
            using var db = Context();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20260915075217_AddMaterialInventoryListings");
            foreach (var (id, role) in new[] { (Buyer, UserRole.BUYER), (OtherBuyer, UserRole.BUYER), (Manager, UserRole.MANAGER), (Seller, UserRole.SELLER) })
                // This fixture intentionally seeds the old schema before testing migrations.
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Users" ("Id", "Email", "PasswordHash", "Role")
                    VALUES ({id}, {id + "@requirements.test"}, 'unused-test-hash', {role.ToString()});
                    """);
            await db.SaveChangesAsync();
            var category = Guid.Parse("00000000-0000-0000-0000-000000000101");
            var listing = new Listing { Id = Guid.NewGuid(), SellerId = Seller, CategoryId = category, Title = "Test stock",
                Quantity = 100, ReservedQuantity = 2, Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(30),
                Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "MaterialRequests" ("Id", "BuyerId", "CategoryId", "Title", "Quantity", "Budget", "DeadlineUtc", "Status")
                VALUES ({LegacyRequest}, {Buyer}, {category}, 'Legacy request', 5, 100, {DateTime.UtcNow.AddDays(7)}, 'MATCHED');
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Matches" ("Id", "MaterialRequestId", "ListingId", "Score")
                VALUES ({Guid.NewGuid()}, {LegacyRequest}, {listing.Id}, {0.5m});
                """);
            db.Reservations.Add(new Reservation { Id = Guid.NewGuid(), MaterialRequestId = LegacyRequest, ListingId = listing.Id, Quantity = 2, Status = ReservationStatus.ACTIVE });
            await db.SaveChangesAsync();
            await migrator.MigrateAsync();
        }
        catch { await DisposeAsync(); throw; }
    }

    public SurplusLinkDbContext Context() => new(new DbContextOptionsBuilder<SurplusLinkDbContext>().UseNpgsql(connection!).Options);

    public WebApplicationFactory<Program> App(IRequirementWorkflowStarter? starter = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SurplusLink"] = connection,
                ["AgentWorkflow:Enabled"] = "true",
                ["AgentWorkflow:SharedToken"] = "workflow-tests-only-token-at-least-32-characters",
                ["Jwt:Issuer"] = "requirements-tests", ["Jwt:Audience"] = "requirements-tests", ["Jwt:Secret"] = Secret,
                ["Jwt:ExpirationMinutes"] = "60", ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Logging:LogLevel:Default"] = "Error"
            }));
            builder.ConfigureServices(services =>
            {
                var worker = services.Single(x => x.ImplementationType == typeof(WorkflowExecutionWorker));
                services.Remove(worker); // Queue tests explicitly control execution; live worker tests restore it.
                if (starter is not null) services.AddSingleton(starter);
            });
        });

    public HttpClient Client(WebApplicationFactory<Program> app, Guid id, string role = "BUYER", params string[] additionalRoles)
    {
        var token = new JwtSecurityToken("requirements-tests", "requirements-tests",
            new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }.Concat(new[] { role }.Concat(additionalRoles).Select(value => new Claim(ClaimTypes.Role, value))),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256));
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public async Task DisposeAsync()
    {
        if (!created) return;
        // Only this fixture's generated database is ever dropped. Never the configured database.
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, "^surpluslink_m2_test_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Unexpected test database name.");
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
        created = false;
    }
}
