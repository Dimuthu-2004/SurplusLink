using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Workflows;

public sealed class AgentWorkflowService(SurplusLinkDbContext db)
{
    public async Task<(IReadOnlyList<AgentWorkflowListItem> Items, int Total)> ListAsync(WorkflowQuery input, CancellationToken ct)
    {
        var query = db.AgentWorkflows.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(input.Status)) query = query.Where(x => x.Status == Enum.Parse<AgentWorkflowStatus>(input.Status, true));
        if (!string.IsNullOrWhiteSpace(input.Search)) { var search = input.Search.Trim(); query = query.Where(x => x.CurrentStage.Contains(search) || (x.Decision != null && x.Decision.Contains(search))); }
        var total = await query.CountAsync(ct);
        var ordered = input.SortBy.ToLowerInvariant() switch
        {
            "status" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? query.OrderBy(x => x.Status).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Status).ThenBy(x => x.Id),
            "stage" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? query.OrderBy(x => x.CurrentStage).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CurrentStage).ThenBy(x => x.Id),
            _ => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? query.OrderBy(x => x.StartedAtUtc).ThenBy(x => x.Id) : query.OrderByDescending(x => x.StartedAtUtc).ThenBy(x => x.Id),
        };
        var rows = await ordered.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToListAsync(ct);
        var items = new List<AgentWorkflowListItem>(rows.Count);
        foreach (var row in rows)
        {
            var group = await GetApprovalGroupAsync(row, ct);
            items.Add(new AgentWorkflowListItem(row.Id, row.MaterialRequestId, row.MaterialMatchId, row.Status,
                row.CurrentStage, row.RetryCount, row.StartedAtUtc, row.CompletedAtUtc, row.ErrorJson,
                group is null ? null : ToSummary(group)));
        }
        return (items, total);
    }

    public async Task<AgentWorkflowResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var workflow = await LoadAsync(id, ct) ?? throw NotFound();
        return ToResponse(workflow) with { ApprovalGroup = await GetApprovalGroupAsync(workflow, ct) };
    }

    public async Task<AgentWorkflowSummary> SummaryAsync(Guid id, CancellationToken ct)
    {
        var workflow = await db.AgentWorkflows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound();
        var statuses = await db.Approvals.AsNoTracking().Where(x => x.AgentWorkflowId == id)
            .GroupBy(x => x.Decision).Select(x => new AgentWorkflowStatusCount(x.Key.ToString(), x.Count()))
            .ToListAsync(ct);
        return new(workflow.Id, workflow.Status, workflow.CurrentStage,
            await db.AgentSteps.CountAsync(x => x.AgentWorkflowId == id, ct),
            await db.AgentToolCalls.CountAsync(x => x.AgentStep.AgentWorkflowId == id, ct),
            workflow.RetryCount, await db.Approvals.CountAsync(x => x.AgentWorkflowId == id, ct),
            workflow.StartedAtUtc, workflow.CompletedAtUtc, statuses);
    }

    public Task<AgentWorkflowResponse> ApproveAsync(Guid id, Guid managerId, string? note, CancellationToken ct) =>
        DecideAsync(id, managerId, ApprovalDecision.APPROVED, note, ct);

    public Task<AgentWorkflowResponse> RejectAsync(Guid id, Guid managerId, string? note, CancellationToken ct) =>
        DecideAsync(id, managerId, ApprovalDecision.REJECTED, note, ct);

    public async Task<AgentWorkflowResponse> ReviseAsync(Guid id, Guid managerId, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var workflow = await LockAsync(id, ct) ?? throw NotFound();
        EnsurePendingApproval(workflow);
        var cleanNote = NormalizeNote(note);
        if (cleanNote.Length == 0) throw new AgentWorkflowException(400, "A revision note is required.");
        workflow.Status = AgentWorkflowStatus.REVISION_REQUESTED;
        workflow.CurrentStage = "REVISION";
        workflow.Decision = cleanNote;
        // A revision must be actionable. A pending approval cannot be edited or
        // matched again by the buyer, whereas OPEN can be amended and restarted.
        if (workflow.MaterialRequestId is Guid requestId)
        {
            var request = await db.BuyerRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new AgentWorkflowException(409, "The workflow material request was not found.");
            if (request.Status is BuyerRequestStatus.MATCH_FOUND or BuyerRequestStatus.PENDING_APPROVAL)
            {
                request.Status = BuyerRequestStatus.OPEN;
                db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(), ActorUserId = managerId, EntityType = nameof(BuyerRequest), EntityId = request.Id,
                    Action = "WORKFLOW_REVISION_REOPENED"
                });
            }
        }
        db.AgentSteps.Add(new AgentStep
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, Sequence = await db.AgentSteps.CountAsync(x => x.AgentWorkflowId == workflow.Id, ct) + 1,
            Stage = "REVISION", Status = "PENDING",
            InputJson = "{}", OutputJson = "{}", ValidationJson = "{}", StartedAtUtc = DateTime.UtcNow
        });
        db.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, DecidedByUserId = managerId, Decision = ApprovalDecision.REVISION_REQUESTED,
            Note = cleanNote, DecidedAtUtc = DateTime.UtcNow
        });
        Audit(workflow.Id, managerId, "WORKFLOW_REVISION_REQUESTED");
        await UpdateParticipationAsync(workflow, managerId, OfferStatus.REVISION_REQUESTED, TransactionStatus.REJECTED, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(await LoadAsync(id, ct) ?? workflow);
    }

    private async Task<AgentWorkflowResponse> DecideAsync(
        Guid id, Guid managerId, ApprovalDecision decision, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var workflow = await LockAsync(id, ct) ?? throw NotFound();
        if (workflow.Status == AgentWorkflowStatus.APPROVED && decision == ApprovalDecision.APPROVED)
        {
            await tx.CommitAsync(ct);
            return ToResponse(await LoadAsync(id, ct) ?? workflow);
        }
        EnsurePendingApproval(workflow);
        var cleanNote = NormalizeNote(note);
        if (decision == ApprovalDecision.REJECTED && cleanNote.Length == 0)
            throw new AgentWorkflowException(400, "A rejection note is required.");

        if (decision == ApprovalDecision.APPROVED)
        {
            // Manager preference cannot override a deterministic validation failure.
            try
            {
                using var validation = System.Text.Json.JsonDocument.Parse(workflow.ValidationJson);
                if (validation.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object ||
                    !validation.RootElement.TryGetProperty("valid", out var valid) || valid.ValueKind != System.Text.Json.JsonValueKind.True)
                    throw new AgentWorkflowException(409, "A valid deterministic validation result is required before approval.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new AgentWorkflowException(409, "Workflow validation data is invalid.");
            }

            var pendingTransactions = await db.Transactions
                .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.Listing)
                .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.MaterialRequest)
                .Where(x => x.Status == TransactionStatus.PENDING_APPROVAL &&
                    (x.ApprovalWorkflowId == workflow.Id ||
                     (x.ApprovalWorkflowId == null &&
                      ((workflow.MaterialRequestId != null && x.Offer.MaterialMatch.MaterialRequestId == workflow.MaterialRequestId) ||
                       x.Offer.MaterialMatchId == workflow.MaterialMatchId))))
                .ToListAsync(ct);

            if (pendingTransactions.Count == 0)
            {
                if (workflow.MaterialMatchId is not Guid fallbackMatchId)
                    throw new AgentWorkflowException(409, "Approval requires a workflow match.");

                var match = await db.Matches.Include(x => x.Listing).Include(x => x.MaterialRequest)
                    .SingleOrDefaultAsync(x => x.Id == fallbackMatchId, ct)
                    ?? throw new AgentWorkflowException(409, "The workflow match was not found.");

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM \"MaterialRequests\" WHERE \"Id\" = {match.MaterialRequestId} FOR UPDATE", ct);
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM \"Listings\" WHERE \"Id\" = {match.ListingId} FOR UPDATE", ct);
                await db.Entry(match.Listing).ReloadAsync(ct);
                await db.Entry(match.MaterialRequest).ReloadAsync(ct);

                if (workflow.MaterialRequestId != match.MaterialRequestId || match.Status != MatchStatus.ROUTED ||
                    match.Distance is null or < 0 || match.DurationMinutes is null or < 0 ||
                    match.EstimatedTransportCost is null or < 0 ||
                    match.Listing.AvailableUntil <= DateTime.UtcNow || match.MaterialRequest.Deadline <= DateTime.UtcNow ||
                    match.DurationMinutes > (decimal)(match.MaterialRequest.Deadline - DateTime.UtcNow).TotalMinutes ||
                    !string.Equals(match.Listing.Unit, match.MaterialRequest.Unit, StringComparison.OrdinalIgnoreCase) ||
                    match.Listing.UnitPrice * match.MaterialRequest.RequiredQuantity + match.EstimatedTransportCost > match.MaterialRequest.MaximumBudget)
                    throw new AgentWorkflowException(409, "The recommendation is no longer eligible. Request a revision.");

                if (MarketplaceMatchPolicy.RejectionReason(match.MaterialRequest.BuyerId, match.Listing.SellerId) is not null)
                    throw new AgentWorkflowException(409, "A workflow cannot approve a self-dealing match.");
                if (match.Listing.Status != ListingStatus.ACTIVE)
                    throw new AgentWorkflowException(409, "The listing is not available for reservation.");
                if (match.MaterialRequest.Status is not BuyerRequestStatus.OPEN and not BuyerRequestStatus.MATCH_FOUND and not BuyerRequestStatus.PENDING_APPROVAL)
                    throw new AgentWorkflowException(409, "The material request is not open for reservation.");
                if (match.Listing.CategoryId != match.MaterialRequest.CategoryId)
                    throw new AgentWorkflowException(409, "The workflow match categories do not match.");

                var existingRes = await db.Reservations.AnyAsync(x => x.ListingId == match.ListingId &&
                    x.MaterialRequestId == match.MaterialRequestId && x.Status != ReservationStatus.RELEASED &&
                    x.Status != ReservationStatus.CANCELLED, ct);
                if (!existingRes)
                {
                    var quantity = match.MaterialRequest.RequiredQuantity;
                    if (match.Listing.ReservedQuantity + quantity > match.Listing.Quantity)
                        throw new AgentWorkflowException(409, "Insufficient available quantity.");
                    match.Listing.ReservedQuantity += quantity;
                    if (match.Listing.ReservedQuantity == match.Listing.Quantity)
                        match.Listing.Status = ListingStatus.RESERVED;
                    db.Reservations.Add(new Reservation
                    {
                        Id = Guid.NewGuid(), ListingId = match.ListingId, MaterialRequestId = match.MaterialRequestId,
                        Quantity = quantity, Status = ReservationStatus.ACTIVE
                    });
                    db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = managerId,
                        EntityType = nameof(Listing), EntityId = match.ListingId, Action = "QUANTITY_RESERVED" });
                }
                match.MaterialRequest.Status = BuyerRequestStatus.APPROVED;
            }
            else
            {
                if (workflow.MaterialRequestId is Guid requestId)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM \"MaterialRequests\" WHERE \"Id\" = {requestId} FOR UPDATE", ct);
                }

                // Deterministic lock order on all involved listings to avoid deadlocks:
                var distinctListingIds = pendingTransactions
                    .Select(x => x.Offer.MaterialMatch.ListingId)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                foreach (var listingId in distinctListingIds)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM \"Listings\" WHERE \"Id\" = {listingId} FOR UPDATE", ct);
                }

                foreach (var txRow in pendingTransactions)
                {
                    await db.Entry(txRow.Offer.MaterialMatch.Listing).ReloadAsync(ct);
                    await db.Entry(txRow.Offer.MaterialMatch.MaterialRequest).ReloadAsync(ct);
                }

                var request = pendingTransactions.First().Offer.MaterialMatch.MaterialRequest;
                if (request.Status is not BuyerRequestStatus.OPEN and not BuyerRequestStatus.MATCH_FOUND and not BuyerRequestStatus.PENDING_APPROVAL)
                    throw new AgentWorkflowException(409, "The material request is not open for reservation.");
                if (request.Deadline <= DateTime.UtcNow)
                    throw new AgentWorkflowException(409, "The material request has expired.");
                if (pendingTransactions.Sum(x => x.Quantity) > request.RequiredQuantity)
                    throw new AgentWorkflowException(409, "Selected allocations exceed the requested quantity.");

                // Validate every transaction allocation
                foreach (var txRow in pendingTransactions)
                {
                    var match = txRow.Offer.MaterialMatch;
                    var listing = match.Listing;
                    var allocatedQty = txRow.Quantity;

                    if (allocatedQty <= 0)
                        throw new AgentWorkflowException(409, "Allocated quantity must be positive.");

                    if (listing.Status != ListingStatus.ACTIVE)
                        throw new AgentWorkflowException(409, $"Listing '{listing.Title}' is not available for reservation.");

                    if (listing.AvailableUntil <= DateTime.UtcNow)
                        throw new AgentWorkflowException(409, $"Listing '{listing.Title}' has expired.");

                    if (listing.CategoryId != request.CategoryId)
                        throw new AgentWorkflowException(409, $"Listing '{listing.Title}' category does not match requirement.");

                    if (!string.Equals(listing.Unit, request.Unit, StringComparison.OrdinalIgnoreCase))
                        throw new AgentWorkflowException(409, $"Listing '{listing.Title}' unit does not match requirement.");

                    if (MarketplaceMatchPolicy.RejectionReason(request.BuyerId, listing.SellerId) is not null)
                        throw new AgentWorkflowException(409, $"A workflow cannot approve a self-dealing match with seller for '{listing.Title}'.");

                    if (match.Status != MatchStatus.ROUTED || match.Distance is null or < 0 || match.DurationMinutes is null or < 0 || match.EstimatedTransportCost is null or < 0)
                        throw new AgentWorkflowException(409, $"The route data for '{listing.Title}' is incomplete.");

                    if (match.DurationMinutes > (decimal)(request.Deadline - DateTime.UtcNow).TotalMinutes)
                        throw new AgentWorkflowException(409, $"Delivery duration exceeds deadline for '{listing.Title}'.");

                    // Terms check:
                    if (txRow.Offer.UnitValue != listing.UnitPrice || txRow.Offer.TotalValue != txRow.Quantity * listing.UnitPrice ||
                        txRow.Offer.BuyerId != request.BuyerId || txRow.Offer.SellerId != listing.SellerId)
                        throw new AgentWorkflowException(409, "The offered terms changed. Request a revision.");

                    // Stock re-check:
                    if (listing.ReservedQuantity + allocatedQty > listing.Quantity)
                        throw new AgentWorkflowException(409, $"STOCK_CHANGED: allocation conflict for listing '{listing.Title}'. Required: {allocatedQty}, Available: {listing.Quantity - listing.ReservedQuantity}.");
                }

                // Total budget check:
                var totalMaterialCost = pendingTransactions.Sum(x => x.Quantity * x.Offer.UnitValue);
                var totalTransportCost = pendingTransactions.Select(x => x.Offer.MaterialMatch).DistinctBy(x => x.Id).Sum(x => x.EstimatedTransportCost ?? 0);
                if (totalMaterialCost + totalTransportCost > request.MaximumBudget)
                    throw new AgentWorkflowException(409, "Total cost across all selections exceeds the requirement budget.");

                // All checks passed! Now reserve exact allocated quantities:
                foreach (var txRow in pendingTransactions)
                {
                    var match = txRow.Offer.MaterialMatch;
                    var listing = match.Listing;
                    var allocatedQty = txRow.Quantity;

                    var existing = await db.Reservations.AnyAsync(x => x.ListingId == listing.Id &&
                        x.MaterialRequestId == request.Id && x.Status != ReservationStatus.RELEASED &&
                        x.Status != ReservationStatus.CANCELLED, ct);

                    if (!existing)
                    {
                        listing.ReservedQuantity += allocatedQty;
                        if (listing.ReservedQuantity == listing.Quantity)
                            listing.Status = ListingStatus.RESERVED;

                        db.Reservations.Add(new Reservation
                        {
                            Id = Guid.NewGuid(),
                            ListingId = listing.Id,
                            MaterialRequestId = request.Id,
                            Quantity = allocatedQty,
                            Status = ReservationStatus.ACTIVE
                        });

                        db.AuditLogs.Add(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            ActorUserId = managerId,
                            EntityType = nameof(Listing),
                            EntityId = listing.Id,
                            Action = "QUANTITY_RESERVED"
                        });
                    }
                }

                request.Status = BuyerRequestStatus.APPROVED;
            }

            workflow.Status = AgentWorkflowStatus.APPROVED;
            workflow.CurrentStage = "APPROVED";
            workflow.CompletedAtUtc = null;
        }
        else
        {
            workflow.Status = AgentWorkflowStatus.REJECTED;
            workflow.CurrentStage = "REJECTED";
            workflow.CompletedAtUtc = DateTime.UtcNow;
            if (workflow.MaterialRequestId is Guid requestId)
            {
                var request = await db.BuyerRequests.SingleAsync(x => x.Id == requestId, ct);
                request.Status = BuyerRequestStatus.REJECTED;
            }
        }

        await UpdateParticipationAsync(workflow, managerId,
            decision == ApprovalDecision.APPROVED ? OfferStatus.ACCEPTED : OfferStatus.REJECTED,
            decision == ApprovalDecision.APPROVED ? TransactionStatus.APPROVED : TransactionStatus.REJECTED, ct);

        workflow.Decision = cleanNote;
        if (workflow.MaterialRequestId is Guid outcomeRequestId)
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = managerId,
                EntityType = nameof(BuyerRequest), EntityId = outcomeRequestId, Action = "WORKFLOW_" + decision });
        db.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(), AgentWorkflowId = workflow.Id, DecidedByUserId = managerId, Decision = decision,
            Note = cleanNote, DecidedAtUtc = DateTime.UtcNow
        });
        Audit(workflow.Id, managerId, $"WORKFLOW_{decision}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(await LoadAsync(id, ct) ?? workflow);
    }

    private async Task<ApprovalGroupResponse?> GetApprovalGroupAsync(AgentWorkflow workflow, CancellationToken ct)
    {
        var baseQuery = db.Transactions.AsNoTracking()
            .Include(x => x.Offer).ThenInclude(x => x.Buyer)
            .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.Listing).ThenInclude(x => x.Seller)
            .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.MaterialRequest);
        var allocations = await baseQuery.Where(x => x.ApprovalWorkflowId == workflow.Id).ToListAsync(ct);
        // The nullable group key was added after the original single-match
        // workflow. Read those rows through their existing request/match link.
        if (allocations.Count == 0)
        {
            allocations = await baseQuery.Where(x => x.ApprovalWorkflowId == null &&
                ((workflow.MaterialRequestId != null && x.Offer.MaterialMatch.MaterialRequestId == workflow.MaterialRequestId) ||
                 x.Offer.MaterialMatchId == workflow.MaterialMatchId)).ToListAsync(ct);
        }
        if (allocations.Count == 0) return null;

        var request = allocations[0].Offer.MaterialMatch.MaterialRequest;
        var selected = allocations.Sum(x => x.Quantity);
        var rows = allocations.OrderBy(x => x.Offer.MaterialMatch.Listing.Title).ThenBy(x => x.Id)
            .Select(x => new ApprovalAllocationResponse(
                x.Id, x.SellerId, x.Offer.MaterialMatch.Listing.Seller.FullName ?? "Seller",
                x.Offer.MaterialMatch.Listing.Seller.BusinessName, x.Offer.MaterialMatch.ListingId,
                x.Offer.MaterialMatch.Listing.Title, x.Quantity,
                x.Offer.MaterialMatch.Listing.Quantity - x.Offer.MaterialMatch.Listing.ReservedQuantity,
                x.Offer.MaterialMatch.Listing.Unit, x.Offer.UnitValue, x.Offer.TotalValue,
                x.Offer.MaterialMatch.Score, x.Offer.MaterialMatch.Distance,
                x.Offer.MaterialMatch.EstimatedTransportCost, x.Status.ToString())).ToArray();
        return new ApprovalGroupResponse(request.Title, allocations[0].Offer.Buyer.FullName ?? "Buyer",
            request.RequiredQuantity, selected, request.RequiredQuantity - selected, request.Unit,
            selected == request.RequiredQuantity ? "FULL" : "PARTIAL", rows.Select(x => x.SellerId).Distinct().Count(),
            rows.Sum(x => x.MaterialValue) + rows.Sum(x => x.TransportCost ?? 0), rows);
    }

    private static ApprovalGroupSummaryResponse ToSummary(ApprovalGroupResponse group) => new(
        group.RequirementTitle, group.BuyerName, group.RequestedQuantity, group.SelectedQuantity,
        group.RemainingQuantity, group.Unit, group.FulfillmentStatus, group.SellerCount, group.TotalValue);

    private async Task<AgentWorkflow?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.AgentWorkflows.AsNoTracking()
            .Include(x => x.MaterialRequest)
            .Include(x => x.Steps).ThenInclude(x => x.ToolCalls)
            .Include(x => x.Approvals).SingleOrDefaultAsync(x => x.Id == id, ct);

    private async Task UpdateParticipationAsync(AgentWorkflow workflow, Guid actor, OfferStatus offerStatus,
        TransactionStatus status, CancellationToken ct)
    {
        var transactions = await db.Transactions.Include(x => x.Offer).Where(x =>
            (x.ApprovalWorkflowId == workflow.Id ||
             (x.ApprovalWorkflowId == null &&
              ((workflow.MaterialRequestId != null && x.Offer.MaterialMatch.MaterialRequestId == workflow.MaterialRequestId) ||
               x.Offer.MaterialMatchId == workflow.MaterialMatchId))) &&
            x.Status == TransactionStatus.PENDING_APPROVAL).ToListAsync(ct);
        foreach (var row in transactions)
        {
            row.Status = status;
            row.Offer.Status = offerStatus;
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor,
                EntityType = nameof(Offer), EntityId = row.OfferId, Action = "OFFER_" + offerStatus });
            row.ReservedQuantity = status == TransactionStatus.APPROVED ? row.Quantity : 0;
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor,
                EntityType = nameof(Transaction), EntityId = row.Id, Action = "WORKFLOW_" + workflow.Status });
        }
    }

    private async Task<AgentWorkflow?> LockAsync(Guid id, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"AgentWorkflows\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        return await db.AgentWorkflows.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    private static void EnsurePendingApproval(AgentWorkflow workflow)
    {
        // A revision invalidates the prior recommendation. It must be rerun through
        // deterministic validation before any later manager decision can reserve.
        if (workflow.Status != AgentWorkflowStatus.PENDING_APPROVAL)
            throw new AgentWorkflowException(409, $"This decision requires PENDING_APPROVAL; current status is {workflow.Status}.");
    }

    private static string NormalizeNote(string? note) => note?.Trim() ?? string.Empty;

    private void Audit(Guid workflowId, Guid managerId, string action) => db.AuditLogs.Add(new AuditLog
    {
        Id = Guid.NewGuid(), ActorUserId = managerId, EntityType = nameof(AgentWorkflow), EntityId = workflowId, Action = action
    });

    private static AgentWorkflowException NotFound() => new(404, "Workflow was not found.");

    private static AgentWorkflowResponse ToResponse(AgentWorkflow workflow) => new(
        workflow.Id, workflow.MaterialRequestId, workflow.MaterialMatchId, workflow.Status, workflow.CurrentStage,
        workflow.InputJson, workflow.OutputJson, workflow.ValidationJson, workflow.ErrorJson, workflow.Decision,
        workflow.RetryCount, workflow.StartedAtUtc, workflow.CompletedAtUtc,
        workflow.Steps.OrderBy(x => x.Sequence).Select(ToStep).ToArray(),
        workflow.Approvals.OrderBy(x => x.DecidedAtUtc).Select(ToApproval).ToArray(),
        workflow.MaterialRequest?.RecommendationReason ?? (workflow.MaterialMatchId.HasValue || (workflow.MaterialRequest?.RecommendedMatchId.HasValue ?? false)
            ? "Highest deterministic final score among valid routed candidates; ties use condition, total estimated cost, distance, then listing ID."
            : null));

    private static AgentStepResponse ToStep(AgentStep step) => new(
        step.Id, step.Sequence, step.Stage, step.Status, step.InputJson, step.OutputJson, step.ValidationJson,
        step.ErrorJson, step.RetryCount, step.StartedAtUtc, step.CompletedAtUtc, step.DurationMilliseconds,
        step.ToolCalls.OrderBy(x => x.StartedAtUtc).Select(ToToolCall).ToArray());

    private static AgentToolCallResponse ToToolCall(AgentToolCall call) => new(
        call.Id, call.ToolName, call.InputJson, call.OutputJson, call.ErrorJson, call.RetryCount,
        call.StartedAtUtc, call.CompletedAtUtc, call.DurationMilliseconds);

    private static ApprovalResponse ToApproval(Approval approval) => new(
        approval.Id, approval.DecidedByUserId, approval.Decision, approval.Note, approval.DecidedAtUtc);
}
