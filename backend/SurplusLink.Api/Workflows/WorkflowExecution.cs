using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Routing;

namespace SurplusLink.Api.Workflows;

public sealed class WorkflowExecutionOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
    public string SharedToken { get; set; } = "";
    public int PollSeconds { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 60;
    public int HttpTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 1;
    public int MaxCandidates { get; set; } = 10;
}

public sealed record WorkflowListingSnapshot(Guid MatchId, Guid ListingId, Guid SellerId, Guid CategoryId,
    decimal AvailableQuantity, string Unit, decimal UnitPrice, string Condition, string Status,
    DateTime AvailableUntil, decimal? Latitude, decimal? Longitude, decimal? DistanceKm,
    decimal? DurationMinutes, decimal? TransportCost, string? RoutingError);
public sealed record WorkflowRunRequest(Guid WorkflowId, object BuyerRequest, IReadOnlyList<WorkflowListingSnapshot> Listings,
    string? Objective = null);
public sealed record WorkflowValidation(bool Valid, bool RequiresApproval, Guid? RecommendedMatchId,
    string[] Violations, string[] Warnings);
public sealed record WorkflowRecommendation(Guid MatchId, Guid ListingId, decimal Score, decimal DistanceKm, decimal TransportCost);
public sealed record WorkflowToolTrace(string ToolName, string Status, JsonElement? Output, string? ErrorCode,
    int RetryCount, DateTime StartedAtUtc, DateTime CompletedAtUtc, long DurationMilliseconds);
public sealed record WorkflowStepTrace(int Sequence, string Stage, string Status, JsonElement Output,
    string? ErrorCode, int RetryCount, DateTime StartedAtUtc, DateTime CompletedAtUtc, long DurationMilliseconds,
    WorkflowToolTrace[] ToolCalls);
public sealed record WorkflowRunResult(Guid WorkflowId, string Status, WorkflowValidation Validation,
    WorkflowRecommendation? Recommendation, WorkflowStepTrace[] Steps, string? ErrorCode);

public interface IAgentWorkflowClient
{
    Task<(WorkflowRunResult Result, int Retries)> RunAsync(WorkflowRunRequest request, CancellationToken ct);
}

public sealed class AgentWorkflowClient(HttpClient client, IOptions<WorkflowExecutionOptions> settings) : IAgentWorkflowClient
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<(WorkflowRunResult Result, int Retries)> RunAsync(WorkflowRunRequest request, CancellationToken ct)
    {
        var options = settings.Value;
        for (var attempt = 0; ; attempt++)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(options.HttpTimeoutSeconds));
            using var message = new HttpRequestMessage(HttpMethod.Post,
                new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), "internal/workflows/run"));
            message.Headers.Add("X-Internal-Token", options.SharedToken);
            message.Content = JsonContent.Create(request, options: Json);
            try
            {
                using var response = await client.SendAsync(message, deadline.Token);
                if (((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
                    && attempt < options.MaxRetries) continue;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<WorkflowRunResult>(Json, deadline.Token)
                    ?? throw new JsonException("Empty workflow result.");
                return (result, attempt);
            }
            catch (HttpRequestException error) when (attempt < options.MaxRetries && !ct.IsCancellationRequested
                && (error.StatusCode is null || (int)error.StatusCode >= 500)) { }
            // A timed-out request may still be running. Do not dispatch a duplicate.
        }
    }
}

/// <summary>
/// Durable queue: the buyer action commits RUNNING/QUEUED; a worker claims one row
/// with SKIP LOCKED. The lock is bounded by TimeoutSeconds and released on process
/// failure, so another worker can replay the read-only graph. Only this API writes.
/// </summary>
public sealed class WorkflowQueueProcessor(SurplusLinkDbContext db, IAgentWorkflowClient client,
    ITransportEstimateService transport, IOptions<WorkflowExecutionOptions> settings)
{
    public async Task<bool> ProcessNextAsync(CancellationToken stop)
    {
        await using var tx = await db.Database.BeginTransactionAsync(stop);
        var workflow = (await db.AgentWorkflows.FromSqlRaw("""
            SELECT * FROM "AgentWorkflows"
            WHERE "Status" = 'RUNNING' AND "CurrentStage" = 'QUEUED'
            ORDER BY "StartedAtUtc", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToListAsync(stop)).SingleOrDefault();
        if (workflow is null) return false;
        var request = await db.BuyerRequests.SingleAsync(x => x.Id == workflow.MaterialRequestId, stop);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stop);
        budget.CancelAfter(TimeSpan.FromSeconds(settings.Value.TimeoutSeconds));
        try
        {
            var snapshot = await SnapshotAsync(workflow, request, budget.Token);
            var (result, retries) = await client.RunAsync(snapshot, budget.Token);
            ValidateResult(snapshot, result);
            // Recheck authoritative stock/prices before publishing an approval recommendation.
            if (result.Recommendation is { } recommendation)
            {
                var listing = await db.Listings.AsNoTracking().SingleAsync(x => x.Id == recommendation.ListingId, budget.Token);
                if (!StillEligible(request, listing, recommendation.TransportCost))
                    throw new InvalidOperationException("Snapshot changed.");
            }
            await PersistAsync(workflow, request, result, retries, snapshot, budget.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // Never persist provider messages, credentials, URLs or raw exception bodies.
            workflow.Status = AgentWorkflowStatus.FAILED;
            workflow.CurrentStage = "FAILED";
            workflow.CompletedAtUtc = DateTime.UtcNow;
            workflow.ErrorJson = JsonSerializer.Serialize(new { code = error is OperationCanceledException ? "WORKFLOW_TIMEOUT" : "WORKFLOW_EXECUTION_FAILED" });
            workflow.ValidationJson = JsonSerializer.Serialize(new WorkflowValidation(false, false, null,
                ["WORKFLOW_EXECUTION_FAILED"], []), AgentWorkflowClient.Json);
            request.Status = BuyerRequestStatus.OPEN; // Buyer may explicitly start a fresh attempt.
        }
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityId = request.Id, EntityType = nameof(BuyerRequest),
            Action = "WORKFLOW_" + workflow.Status });
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityId = workflow.Id, EntityType = nameof(AgentWorkflow),
            Action = "WORKFLOW_" + workflow.Status });
        await db.SaveChangesAsync(stop);
        await tx.CommitAsync(stop);
        return true;
    }

    private async Task<WorkflowRunRequest> SnapshotAsync(AgentWorkflow workflow, BuyerRequest request, CancellationToken ct)
    {
        var listings = await db.Listings.AsNoTracking().Where(x => x.CategoryId == request.CategoryId &&
            x.SellerId != request.BuyerId && x.Status == ListingStatus.ACTIVE)
            .OrderByDescending(x => x.AvailableUntil >= request.Deadline &&
                x.Quantity - x.ReservedQuantity >= request.RequiredQuantity && x.Unit.ToLower() == request.Unit.ToLower() &&
                x.UnitPrice * request.RequiredQuantity <= request.MaximumBudget)
            .ThenBy(x => x.UnitPrice).ThenBy(x => x.Id).Take(settings.Value.MaxCandidates).ToListAsync(ct);
        var existing = await db.Matches.AsNoTracking().Where(x => x.MaterialRequestId == request.Id)
            .ToDictionaryAsync(x => x.ListingId, x => x.Id, ct);
        var rows = new List<WorkflowListingSnapshot>();
        foreach (var listing in listings)
        {
            TransportEstimate? estimate = null;
            if (listing.Latitude.HasValue && listing.Longitude.HasValue && request.Latitude.HasValue && request.Longitude.HasValue)
                estimate = await transport.EstimateAsync(new RouteRequest(listing.Latitude.Value, listing.Longitude.Value,
                    request.Latitude.Value, request.Longitude.Value), ct);
            rows.Add(new(existing.GetValueOrDefault(listing.Id, Guid.NewGuid()), listing.Id, listing.SellerId,
                listing.CategoryId, listing.Quantity - listing.ReservedQuantity, listing.Unit, listing.UnitPrice,
                listing.Condition.ToString(), listing.Status.ToString(), listing.AvailableUntil, listing.Latitude,
                listing.Longitude, estimate?.Route.DistanceKm, estimate?.Route.DurationMinutes,
                estimate?.EstimatedTransportCost, estimate?.ErrorCode));
        }
        return new(workflow.Id, new { id = request.Id, buyerId = request.BuyerId, categoryId = request.CategoryId,
            requiredQuantity = request.RequiredQuantity, unit = request.Unit, maximumBudget = request.MaximumBudget,
            deadline = request.Deadline, latitude = request.Latitude, longitude = request.Longitude,
            notes = request.Notes, status = request.Status.ToString() }, rows,
            request.Title.Length <= 2000 ? request.Title : request.Title[..2000]);
    }

    internal static bool StillEligible(BuyerRequest request, Listing listing, decimal transportCost) =>
        request.Status == BuyerRequestStatus.MATCHING && request.Deadline > DateTime.UtcNow &&
        listing.Status == ListingStatus.ACTIVE && listing.AvailableUntil >= request.Deadline &&
        listing.SellerId != request.BuyerId && listing.CategoryId == request.CategoryId &&
        string.Equals(listing.Unit, request.Unit, StringComparison.OrdinalIgnoreCase) &&
        listing.Quantity - listing.ReservedQuantity >= request.RequiredQuantity && transportCost >= 0 &&
        listing.UnitPrice * request.RequiredQuantity + transportCost <= request.MaximumBudget;

    internal static void ValidateResult(WorkflowRunRequest input, WorkflowRunResult result)
    {
        var allowedStatuses = new[] { "PENDING_APPROVAL", "REVISION_REQUESTED", "REJECTED", "FAILED" };
        var stages = new[] { "PLANNER", "MATCHING", "LOGISTICS", "VALIDATION" };
        var validationTools = new[] { "check_listing_active", "check_listing_not_expired", "check_available_quantity",
            "check_budget", "check_match_data_complete", "check_transaction_threshold" };
        var toolsByStage = new Dictionary<string, string[]>
        {
            ["PLANNER"] = [],
            ["MATCHING"] = ["search_active_materials", "get_material_detail"],
            ["LOGISTICS"] = ["get_listing_location", "get_route_estimate", "calculate_transport_estimate"],
            ["VALIDATION"] = validationTools,
        };
        if (result.WorkflowId != input.WorkflowId || !allowedStatuses.Contains(result.Status) || result.Validation is null ||
            result.Steps is null || result.Steps.Length > 4 || result.Validation.Violations is null || result.Validation.Warnings is null)
            throw new JsonException("Invalid workflow envelope.");
        foreach (var (step, index) in result.Steps.Select((step, index) => (step, index)))
        {
            if (step is null || step.Stage != stages[index] || step.Sequence != index + 1 || step.RetryCount is < 0 or > 3 ||
                step.DurationMilliseconds < 0 || step.CompletedAtUtc < step.StartedAtUtc || step.ToolCalls is null ||
                step.Status is not ("COMPLETED" or "FAILED") || step.Output.ValueKind != JsonValueKind.Object)
                throw new JsonException("Invalid step trace.");
            if (step.ToolCalls.Any(call => call is null || !toolsByStage[step.Stage].Contains(call.ToolName) || call.RetryCount is < 0 or > 3 ||
                call.DurationMilliseconds < 0 || call.CompletedAtUtc < call.StartedAtUtc ||
                call.Status is not ("COMPLETED" or "FAILED"))) throw new JsonException("Invalid tool trace.");
        }
        var pending = result.Status == "PENDING_APPROVAL";
        if (pending != result.Validation.Valid || pending != result.Validation.RequiresApproval)
            throw new JsonException("Invalid approval gate.");
        if (!pending)
        {
            if (result.Recommendation is not null || result.Validation.RecommendedMatchId is not null)
                throw new JsonException("Failed validation cannot recommend a match.");
            return;
        }
        if (result.ErrorCode is not null || result.Validation.Violations.Length != 0 || result.Steps.Length != 4 ||
            result.Steps.Any(x => x.Status != "COMPLETED" || x.ErrorCode is not null))
            throw new JsonException("Incomplete approval validation.");
        var calls = result.Steps[3].ToolCalls;
        if (!calls.Select(x => x.ToolName).SequenceEqual(validationTools) || calls.Any(x => x.ErrorCode is not null ||
            x.Status != "COMPLETED" || x.Output is not { ValueKind: JsonValueKind.Object } output ||
            !output.TryGetProperty("passed", out var passed) || passed.ValueKind != JsonValueKind.True))
            throw new JsonException("Deterministic checks must all pass.");
        var rec = result.Recommendation ?? throw new JsonException("Missing recommendation.");
        var listing = input.Listings.SingleOrDefault(x => x.ListingId == rec.ListingId && x.MatchId == rec.MatchId);
        if (listing is null || rec.MatchId != result.Validation.RecommendedMatchId || rec.Score is < 0 or > 1 ||
            rec.DistanceKm < 0 || rec.TransportCost < 0 || rec.DistanceKm != listing.DistanceKm || rec.TransportCost != listing.TransportCost)
            throw new JsonException("Recommendation must reference the trusted snapshot.");
    }

    private async Task PersistAsync(AgentWorkflow workflow, BuyerRequest request, WorkflowRunResult result, int retries,
        WorkflowRunRequest snapshot, CancellationToken ct)
    {
        // Complete potentially failing reads before modifying tracked entities.
        var existingMatch = result.Recommendation is { } selected
            ? await db.Matches.SingleOrDefaultAsync(x => x.Id == selected.MatchId, ct) : null;
        var listing = result.Recommendation is { } chosen
            ? await db.Listings.AsNoTracking().SingleAsync(x => x.Id == chosen.ListingId, ct) : null;
        var persisted = await db.Matches.Where(x => x.MaterialRequestId == request.Id).ToDictionaryAsync(x => x.Id, ct);
        foreach (var row in snapshot.Listings)
        {
            var reason = row.AvailableUntil < request.Deadline ? "LISTING_EXPIRES_BEFORE_DELIVERY"
                : !string.Equals(row.Unit, request.Unit, StringComparison.OrdinalIgnoreCase) ? "UNIT_MISMATCH"
                : row.AvailableQuantity < request.RequiredQuantity ? "INSUFFICIENT_QUANTITY"
                : row.UnitPrice * request.RequiredQuantity > request.MaximumBudget ? "BUDGET_EXCEEDED"
                : row.DistanceKm is null || row.DurationMinutes is null || row.TransportCost is null ? "ROUTING_UNAVAILABLE"
                : row.UnitPrice * request.RequiredQuantity + row.TransportCost > request.MaximumBudget ? "TOTAL_COST_EXCEEDS_BUDGET"
                : null;
            if (!persisted.TryGetValue(row.MatchId, out var candidate))
            {
                candidate = new MaterialMatch { Id = row.MatchId, MaterialRequestId = request.Id, ListingId = row.ListingId };
                db.Matches.Add(candidate);
                persisted.Add(candidate.Id, candidate);
                db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(MaterialMatch),
                    EntityId = candidate.Id, Action = "GENERATE" });
            }
            if (row.DurationMinutes > (decimal)(request.Deadline - DateTime.UtcNow).TotalMinutes)
                reason ??= "DELIVERY_DEADLINE_EXCEEDED";
            candidate.Status = reason is null ? MatchStatus.ROUTED : MatchStatus.REJECTED;
            candidate.RejectionReason = reason;
            candidate.Distance = row.DistanceKm;
            candidate.DurationMinutes = row.DurationMinutes;
            candidate.EstimatedTransportCost = row.TransportCost;
            candidate.Score = Math.Round((50m + 30m * (request.MaximumBudget - row.UnitPrice * request.RequiredQuantity) /
                request.MaximumBudget + 20m * Math.Min((row.AvailableQuantity - request.RequiredQuantity) / request.RequiredQuantity, 1m)) / 100m, 4);
            candidate.Score = Math.Clamp(candidate.Score, 0m, 1m);
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(MaterialMatch), EntityId = candidate.Id,
                Action = row.DistanceKm is not null && row.DurationMinutes is not null && row.TransportCost is not null
                    ? "ROUTE_SUCCEEDED" : "ROUTE_FAILED" });
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(MaterialMatch), EntityId = candidate.Id,
                Action = reason is null ? "RANK" : "REJECT" });
        }
        workflow.Status = Enum.Parse<AgentWorkflowStatus>(result.Status);
        workflow.CurrentStage = result.Status;
        workflow.CompletedAtUtc = DateTime.UtcNow;
        workflow.OutputJson = JsonSerializer.Serialize(result, AgentWorkflowClient.Json);
        workflow.ValidationJson = JsonSerializer.Serialize(result.Validation, AgentWorkflowClient.Json);
        workflow.ErrorJson = result.ErrorCode is null ? null : JsonSerializer.Serialize(new { code = result.ErrorCode });
        workflow.RetryCount = retries + result.Steps.Sum(x => x.RetryCount + x.ToolCalls.Sum(t => t.RetryCount));
        if (result.Recommendation is { } rec)
        {
            var match = persisted.GetValueOrDefault(rec.MatchId) ?? existingMatch;
            if (match is null)
            {
                match = new MaterialMatch { Id = rec.MatchId, ListingId = rec.ListingId, MaterialRequestId = request.Id };
                db.Matches.Add(match);
            }
            match.Score = rec.Score; match.Distance = rec.DistanceKm; match.EstimatedTransportCost = rec.TransportCost;
            match.Status = MatchStatus.ROUTED; match.RejectionReason = null;
            workflow.MaterialMatchId = match.Id;
            var offer = new Offer
            {
                Id = Guid.NewGuid(), MaterialMatchId = match.Id, BuyerId = request.BuyerId,
                SellerId = listing!.SellerId, Quantity = request.RequiredQuantity, UnitValue = listing.UnitPrice,
                TotalValue = request.RequiredQuantity * listing.UnitPrice, Status = OfferStatus.PENDING
            };
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(), OfferId = offer.Id, BuyerId = offer.BuyerId, SellerId = offer.SellerId,
                Quantity = offer.Quantity, TotalValue = offer.TotalValue, Status = TransactionStatus.PENDING_APPROVAL
            };
            db.Offers.Add(offer);
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(Offer), EntityId = offer.Id, Action = "OFFER_CREATED" });
            db.Transactions.Add(transaction);
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(Transaction),
                EntityId = transaction.Id, Action = "PENDING_APPROVAL" });
        }
        // Existing manager approval requires an OPEN/MATCH_FOUND requirement.
        request.Status = result.Status == "PENDING_APPROVAL" ? BuyerRequestStatus.MATCH_FOUND : BuyerRequestStatus.OPEN;
        foreach (var trace in result.Steps)
        {
            var step = new AgentStep { Id = Guid.NewGuid(), Sequence = trace.Sequence, Stage = trace.Stage,
                Status = trace.Status, InputJson = workflow.InputJson, OutputJson = trace.Output.GetRawText(),
                ValidationJson = trace.Stage == "VALIDATION" ? workflow.ValidationJson : "{}",
                ErrorJson = trace.ErrorCode is null ? null : JsonSerializer.Serialize(new { code = trace.ErrorCode }),
                RetryCount = trace.RetryCount, StartedAtUtc = trace.StartedAtUtc, CompletedAtUtc = trace.CompletedAtUtc,
                DurationMilliseconds = trace.DurationMilliseconds };
            foreach (var call in trace.ToolCalls)
                step.ToolCalls.Add(new AgentToolCall { Id = Guid.NewGuid(), ToolName = call.ToolName,
                    InputJson = workflow.InputJson, OutputJson = call.Output?.GetRawText() ?? "{}",
                    ErrorJson = call.ErrorCode is null ? null : JsonSerializer.Serialize(new { code = call.ErrorCode }),
                    RetryCount = call.RetryCount, StartedAtUtc = call.StartedAtUtc, CompletedAtUtc = call.CompletedAtUtc,
                    DurationMilliseconds = call.DurationMilliseconds });
            workflow.Steps.Add(step);
            // Assigned IDs on new children of a tracked workflow need explicit Added state.
            db.AgentSteps.Add(step);
        }
    }
}

public sealed class WorkflowExecutionWorker(IServiceScopeFactory scopes, IOptions<WorkflowExecutionOptions> options,
    ILogger<WorkflowExecutionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<WorkflowQueueProcessor>().ProcessNextAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Workflow queue unavailable; retrying on the next poll."); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
