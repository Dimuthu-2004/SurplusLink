using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Routing;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class WorkflowExecutionTests
{
    internal static readonly string[] Tools = ["check_listing_active", "check_listing_not_expired", "check_available_quantity",
        "check_budget", "check_match_data_complete", "check_transaction_threshold"];

    internal static WorkflowRunResult Success(WorkflowRunRequest request)
    {
        var row = request.Listings[0];
        var now = DateTime.UtcNow;
        var empty = JsonSerializer.SerializeToElement(new { });
        var calls = Tools.Select(name => new WorkflowToolTrace(name, "COMPLETED",
            JsonSerializer.SerializeToElement(new { passed = true, code = "CHECKED", warning = (string?)null }), null, 0, now, now, 1)).ToArray();
        var steps = new[] { "PLANNER", "MATCHING", "LOGISTICS", "VALIDATION" }.Select((name, index) =>
            new WorkflowStepTrace(index + 1, name, "COMPLETED", empty, null, 0, now, now, 1,
                name == "VALIDATION" ? calls : [])).ToArray();
        return new(request.WorkflowId, "PENDING_APPROVAL", new(true, true, row.MatchId, [], []),
            new(row.MatchId, row.ListingId, .85m, row.DistanceKm!.Value, row.TransportCost!.Value), steps, null);
    }

    private static WorkflowRunRequest Request() => new(Guid.NewGuid(), new { }, [new(Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), 20, "kg", 100, "GOOD", "ACTIVE", DateTime.UtcNow.AddDays(10), 6, 79, 10, 30, 500, null)]);

    [Fact]
    public void Result_gate_rejects_forged_validity_missing_checks_ids_and_measurements()
    {
        var request = Request();
        var result = Success(request);
        WorkflowQueueProcessor.ValidateResult(request, result);
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result with { WorkflowId = Guid.NewGuid() }));
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result with { Status = "APPROVED" }));
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result with { Steps = [] }));
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result with {
            Validation = result.Validation with { Valid = false } }));
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result with {
            Recommendation = result.Recommendation! with { TransportCost = 0 } }));
        result.Steps[3].ToolCalls[0] = result.Steps[3].ToolCalls[0] with { Output = JsonSerializer.SerializeToElement(new { passed = false }) };
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateResult(request, result));
    }

    [Fact]
    public async Task Http_client_authenticates_retries_transient_failure_and_roundtrips_decimal_result()
    {
        var request = Request();
        var handler = new Handler(async (message, count, ct) =>
        {
            Assert.EndsWith("/internal/workflows/run", message.RequestUri!.AbsoluteUri);
            Assert.Equal("test-token", message.Headers.GetValues("X-Internal-Token").Single());
            var body = await message.Content!.ReadFromJsonAsync<WorkflowRunRequest>(cancellationToken: ct);
            Assert.Equal(request.WorkflowId, body!.WorkflowId);
            return count == 1 ? new(HttpStatusCode.ServiceUnavailable) :
                new(HttpStatusCode.OK) { Content = JsonContent.Create(Success(request), options: AgentWorkflowClient.Json) };
        });
        var client = new AgentWorkflowClient(new HttpClient(handler), Options.Create(new WorkflowExecutionOptions { SharedToken = "test-token" }));
        var (result, retries) = await client.RunAsync(request, default);
        Assert.Equal(1, retries);
        WorkflowQueueProcessor.ValidateResult(request, result);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task Permanent_http_failure_is_not_retried(HttpStatusCode status)
    {
        var handler = new Handler((_, _, _) => Task.FromResult(new HttpResponseMessage(status)));
        var client = new AgentWorkflowClient(new HttpClient(handler), Options.Create(new WorkflowExecutionOptions()));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.RunAsync(Request(), default));
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task Http_timeout_does_not_dispatch_duplicate_work()
    {
        var handler = new Handler(async (_, _, ct) => { await Task.Delay(5000, ct); return new(HttpStatusCode.OK); });
        var client = new AgentWorkflowClient(new HttpClient(handler), Options.Create(new WorkflowExecutionOptions { HttpTimeoutSeconds = 1 }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.RunAsync(Request(), default));
        Assert.Equal(1, handler.Count);
    }

    private sealed class Handler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Count;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ++Count, ct);
    }
}

public sealed class WorkflowPersistenceTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [LiveWorkflowFact]
    public async Task Actual_FastApi_graph_maps_back_to_postgres_without_inventory_mutations()
    {
        var id = await Seed();
        using var db = fixture.Context();
        var options = Options.Create(new WorkflowExecutionOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("SURPLUSLINK_AI_TEST_URL")!,
            SharedToken = Environment.GetEnvironmentVariable("AI_SERVICE_SHARED_TOKEN")!
        });
        using var http = new HttpClient();
        var client = new AgentWorkflowClient(http, options);
        Assert.True(await new WorkflowQueueProcessor(db, client, new DemoTransport(), options).ProcessNextAsync(default));
        var row = await db.AgentWorkflows.Include(x => x.Steps).ThenInclude(x => x.ToolCalls).SingleAsync(x => x.MaterialRequestId == id);
        Assert.True(row.Status == AgentWorkflowStatus.PENDING_APPROVAL, row.ErrorJson ?? row.ValidationJson);
        Assert.Equal(new[] { "PLANNER", "MATCHING", "LOGISTICS", "VALIDATION" }, row.Steps.OrderBy(x => x.Sequence).Select(x => x.Stage));
        Assert.Equal(6, row.Steps.Single(x => x.Stage == "VALIDATION").ToolCalls.Count);
        Assert.True(JsonDocument.Parse(row.ValidationJson).RootElement.GetProperty("valid").GetBoolean());
        Assert.NotNull(row.MaterialMatchId);
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == id));
        Assert.Equal(0, await db.Listings.Where(x => x.CategoryId == db.BuyerRequests.Where(r => r.Id == id).Select(r => r.CategoryId).First()).SumAsync(x => x.ReservedQuantity));
    }

    [PostgresFact]
    public async Task Queue_to_approval_persists_real_steps_and_tools_without_reserving_and_only_runs_once()
    {
        var id = await Seed();
        using var db = fixture.Context();
        var client = new FakeClient(WorkflowExecutionTests.Success);
        var processor = new WorkflowQueueProcessor(db, client, new DemoTransport(), Options.Create(new WorkflowExecutionOptions()));
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.False(await processor.ProcessNextAsync(default));
        var workflow = await db.AgentWorkflows.Include(x => x.Steps).ThenInclude(x => x.ToolCalls).SingleAsync(x => x.MaterialRequestId == id);
        Assert.Equal(AgentWorkflowStatus.PENDING_APPROVAL, workflow.Status);
        Assert.NotNull(workflow.MaterialMatchId);
        Assert.Equal(4, workflow.Steps.Count);
        Assert.Equal(6, workflow.Steps.Single(x => x.Stage == "VALIDATION").ToolCalls.Count);
        Assert.Equal(BuyerRequestStatus.MATCH_FOUND, (await db.BuyerRequests.FindAsync(id))!.Status);
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == id));
        Assert.Equal(0, await db.Listings.Where(x => x.CategoryId == db.BuyerRequests.Where(r => r.Id == id).Select(r => r.CategoryId).First()).SumAsync(x => x.ReservedQuantity));
        Assert.Equal(1, client.Calls);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == workflow.Id && x.Action == "WORKFLOW_PENDING_APPROVAL"));
    }

    [PostgresFact]
    public async Task Invalid_validation_persists_revision_and_reopens_request_for_explicit_retry()
    {
        var id = await Seed();
        using var db = fixture.Context();
        var client = new FakeClient(request => new(request.WorkflowId, "REVISION_REQUESTED",
            new(false, false, null, ["TOTAL_COST_EXCEEDS_BUDGET_OR_UNKNOWN"], []), null, [], null));
        Assert.True(await new WorkflowQueueProcessor(db, client, new DemoTransport(), Options.Create(new WorkflowExecutionOptions())).ProcessNextAsync(default));
        var row = await db.AgentWorkflows.SingleAsync(x => x.MaterialRequestId == id);
        Assert.Equal(AgentWorkflowStatus.REVISION_REQUESTED, row.Status);
        Assert.Null(row.MaterialMatchId);
        Assert.Equal(BuyerRequestStatus.OPEN, (await db.BuyerRequests.FindAsync(id))!.Status);
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == id));
        var approval = await Assert.ThrowsAsync<AgentWorkflowException>(() => new AgentWorkflowService(db).ApproveAsync(row.Id, fixture.Manager, null, default));
        Assert.Equal(409, approval.StatusCode);
        // Use a new context after the rejected decision transaction.
        using var retryDb = fixture.Context();
        var starter = new PersistentRequirementWorkflowStarter(retryDb);
        var retry = await new RequirementService(retryDb, starter).StartMatchingAsync(id, fixture.Buyer, default);
        Assert.NotEqual(row.Id, retry.WorkflowId);
        await new WorkflowQueueProcessor(retryDb, client, new DemoTransport(), Options.Create(new WorkflowExecutionOptions())).ProcessNextAsync(default);
    }

    [PostgresFact]
    public async Task Transport_or_internal_service_failure_is_terminal_safe_and_does_not_leak_secrets()
    {
        var id = await Seed();
        using var db = fixture.Context();
        var client = new FakeClient(_ => throw new HttpRequestException("secret password and provider URL"));
        Assert.True(await new WorkflowQueueProcessor(db, client, new DemoTransport(), Options.Create(new WorkflowExecutionOptions())).ProcessNextAsync(default));
        var row = await db.AgentWorkflows.SingleAsync(x => x.MaterialRequestId == id);
        Assert.Equal(AgentWorkflowStatus.FAILED, row.Status);
        Assert.DoesNotContain("secret", row.ErrorJson!);
        Assert.Null(row.MaterialMatchId);
        Assert.Equal(BuyerRequestStatus.OPEN, (await db.BuyerRequests.FindAsync(id))!.Status);
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == id));
    }

    private async Task<Guid> Seed()
    {
        using var db = fixture.Context();
        var category = new Category { Id = Guid.NewGuid(), Name = "Workflow demo " + Guid.NewGuid() };
        var request = new BuyerRequest { Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id,
            Title = "Demo requirement", RequiredQuantity = 10, MaximumBudget = 2000, Unit = "kg", Latitude = 6.9m,
            Longitude = 79.8m, Deadline = DateTime.UtcNow.AddDays(7), Status = BuyerRequestStatus.OPEN };
        db.AddRange(category, request, new Listing { Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id,
            Title = "Demo material", Quantity = 20, UnitPrice = 100, Unit = "kg", Condition = MaterialCondition.GOOD,
            Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(30), Latitude = 6.8m, Longitude = 79.9m });
        await db.SaveChangesAsync();
        await new RequirementService(db, new PersistentRequirementWorkflowStarter(db)).StartMatchingAsync(request.Id, fixture.Buyer, default);
        return request.Id;
    }

    private sealed class DemoTransport : ITransportEstimateService
    {
        public Task<TransportEstimate> EstimateAsync(RouteRequest request, CancellationToken ct) =>
            Task.FromResult(new TransportEstimate(new RouteResult(10, 30), 500));
    }
    private sealed class FakeClient(Func<WorkflowRunRequest, WorkflowRunResult> run) : IAgentWorkflowClient
    {
        public int Calls;
        public Task<(WorkflowRunResult Result, int Retries)> RunAsync(WorkflowRunRequest request, CancellationToken ct)
        { Calls++; return Task.FromResult((run(request), 0)); }
    }
}

public sealed class LiveWorkflowFactAttribute : FactAttribute
{
    public LiveWorkflowFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SURPLUSLINK_TEST_CONNECTION")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SURPLUSLINK_AI_TEST_URL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AI_SERVICE_SHARED_TOKEN")))
            Skip = "Set the isolated PostgreSQL test connection, running FastAPI test URL and shared test token.";
    }
}
