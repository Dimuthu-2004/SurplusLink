using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Transactions;

public sealed class TransactionService(SurplusLinkDbContext db)
{
    public async Task<OfferResponse> GetOfferAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var row = await db.Offers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Offer was not found.");
        if (!manager && row.BuyerId != actor && row.SellerId != actor)
            throw new TransactionOperationException(403, "This offer belongs to other participants.");
        return new(row.Id, row.MaterialMatchId, row.BuyerId, row.SellerId, row.Quantity, row.UnitValue,
            row.TotalValue, row.Status, row.CreatedAtUtc, row.UpdatedAtUtc);
    }

    public async Task<TransactionResponse> GetAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await AuthorizeHistoryAsync(id, actor, manager, ct);
        var row = await db.Transactions.AsNoTracking().Include(x => x.Buyer).Include(x => x.Seller).SingleAsync(x => x.Id == id, ct);
        var response = ToResponse(row);
        if ((row.BuyerId == actor || row.SellerId == actor) &&
            row.Status is TransactionStatus.APPROVED or TransactionStatus.HANDED_OVER or TransactionStatus.COMPLETED)
            response = response with {
                BuyerContact = new(row.Buyer.FullName, row.Buyer.Email, row.Buyer.PhoneNumber),
                SellerContact = new(row.Seller.FullName, row.Seller.Email, row.Seller.PhoneNumber)
            };
        return response;
    }

    public async Task AuthorizeHistoryAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var row = await db.Transactions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (!manager && row.BuyerId != actor && row.SellerId != actor)
            throw new TransactionOperationException(403, "This transaction belongs to other participants.");
    }

    public async Task<(IReadOnlyList<OfferResponse> Items, int Total)> ListOffersAsync(OfferQuery input, CancellationToken ct)
    {
        Validate(input);
        var query = db.Offers.AsNoTracking().AsQueryable();
        query = Filter(query, input.Status, input.CreatedFrom, input.CreatedTo, input.UserId);
        var total = await query.CountAsync(ct);
        var ordered = SortOffers(query, input);
        var items = await ordered.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new OfferResponse(x.Id, x.MaterialMatchId, x.BuyerId, x.SellerId, x.Quantity, x.UnitValue, x.TotalValue,
                x.Status, x.CreatedAtUtc, x.UpdatedAtUtc)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<TransactionResponse> Items, int Total)> ListTransactionsAsync(TransactionQuery input, CancellationToken ct)
    {
        Validate(input);
        var query = db.Transactions.AsNoTracking().AsQueryable();
        if (input.OfferId.HasValue) query = query.Where(x => x.OfferId == input.OfferId.Value);
        if (input.MatchId.HasValue) query = query.Where(x => x.Offer.MaterialMatchId == input.MatchId.Value);
        query = Filter(query, input.Status, input.CreatedFrom, input.CreatedTo, input.UserId);
        var total = await query.CountAsync(ct);
        var ordered = SortTransactions(query, input);
        var items = await ordered.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new TransactionResponse(x.Id, x.OfferId, x.BuyerId, x.SellerId, x.Quantity, x.TotalValue,
                x.ReservedQuantity, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.CompletedAtUtc, null, null)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<TransactionHistoryPage> HistoryAsync(Guid id, TransactionQuery input, CancellationToken ct)
    {
        Validate(input);
        if (!await db.Transactions.AnyAsync(x => x.Id == id, ct)) throw new TransactionOperationException(404, "Transaction was not found.");
        var query = db.AuditLogs.AsNoTracking().Where(x => x.EntityType == nameof(Transaction) && x.EntityId == id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((input.Page - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new TransactionHistoryEntry(x.Id, x.ActorUserId, x.Action, x.CreatedAtUtc, null)).ToListAsync(ct);
        return new(items, total, Pages(total, input.PageSize), input.Page, input.PageSize);
    }

    public async Task<TransactionAnalyticsSummary> AnalyticsAsync(CancellationToken ct)
    {
        var pending = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.PENDING_APPROVAL, ct);
        var approved = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.APPROVED, ct);
        var rejected = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.REJECTED, ct);
        var completed = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.COMPLETED, ct);
        var handedOver = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.HANDED_OVER, ct);
        var totalDecisions = approved + handedOver + rejected + completed;
        return new(pending, approved, rejected,
            await db.Transactions.SumAsync(x => (decimal?)x.ReservedQuantity, ct) ?? 0,
            completed, await db.Transactions.Where(x => x.Status == TransactionStatus.COMPLETED)
                .SumAsync(x => (decimal?)x.TotalValue, ct) ?? 0,
            totalDecisions == 0 ? null : (double)completed / totalDecisions);
    }

    public Task DecideOfferAsync(Guid id, Guid actor, OfferStatus status, CancellationToken ct) =>
        UpdateOfferAsync(id, actor, status, ct);

    public async Task<TransactionResponse> ApproveAsync(Guid id, Guid actor, CancellationToken ct)
    {
        var transaction = await db.Transactions.Include(x => x.Offer).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.Status is not (TransactionStatus.PENDING_APPROVAL or TransactionStatus.APPROVED))
            throw new TransactionOperationException(409, "Only pending or already approved transactions can be approved.");
        var workflow = await db.AgentWorkflows.Where(x => x.MaterialMatchId == transaction.Offer.MaterialMatchId &&
            (x.Status == AgentWorkflowStatus.PENDING_APPROVAL || x.Status == AgentWorkflowStatus.APPROVED))
            .OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(ct)
            ?? throw new TransactionOperationException(409, "A validated pending workflow is required before approval.");
        try { await new Workflows.AgentWorkflowService(db).ApproveAsync(workflow.Id, actor, null, ct); }
        catch (Workflows.AgentWorkflowException ex) { throw new TransactionOperationException(ex.StatusCode, ex.Message); }
        await db.Entry(transaction).ReloadAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> HandoverAsync(Guid id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.SellerId != actor) throw new TransactionOperationException(403, "Only the seller can hand over materials.");
        if (transaction.Status != TransactionStatus.APPROVED) throw new TransactionOperationException(409, "Only approved transactions can be handed over.");
        transaction.Status = TransactionStatus.HANDED_OVER;
        db.AuditLogs.Add(Log(id, actor, "TRANSACTION_HANDED_OVER"));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    private Task<int> LockTransactionAsync(Guid id, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Transactions\" WHERE \"Id\" = {id} FOR UPDATE", ct);

    public async Task<TransactionResponse> CompleteAsync(Guid id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await db.Transactions.Include(x => x.Offer).ThenInclude(x => x.MaterialMatch)
            .ThenInclude(x => x.MaterialRequest).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.BuyerId != actor) throw new TransactionOperationException(403, "Only the buyer can confirm receipt.");
        if (transaction.Status != TransactionStatus.HANDED_OVER) throw new TransactionOperationException(409, "Materials must be handed over before receipt can be confirmed.");
        transaction.Status = TransactionStatus.COMPLETED;
        transaction.CompletedAtUtc = DateTime.UtcNow;
        transaction.Offer.MaterialMatch.MaterialRequest.Status = BuyerRequestStatus.COMPLETED;
        var match = transaction.Offer.MaterialMatch;
        var reservations = await db.Reservations.Where(x => x.MaterialRequestId == match.MaterialRequestId &&
            x.ListingId == match.ListingId && x.Status == ReservationStatus.ACTIVE).ToListAsync(ct);
        foreach (var row in reservations) row.Status = ReservationStatus.CONFIRMED;
        var workflows = await db.AgentWorkflows.Where(x => x.MaterialMatchId == match.Id && x.Status == AgentWorkflowStatus.APPROVED).ToListAsync(ct);
        foreach (var row in workflows) { row.Status = AgentWorkflowStatus.COMPLETED; row.CurrentStage = "COMPLETED"; row.CompletedAtUtc = DateTime.UtcNow; }
        db.AuditLogs.Add(Log(id, actor, "TRANSACTION_COMPLETED"));
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor,
            EntityType = nameof(BuyerRequest), EntityId = match.MaterialRequestId, Action = "TRANSACTION_COMPLETED" });
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor,
            EntityType = nameof(Listing), EntityId = match.ListingId, Action = "TRANSFER_COMPLETED" });
        foreach (var row in workflows)
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor,
                EntityType = nameof(AgentWorkflow), EntityId = row.Id, Action = "WORKFLOW_COMPLETED" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> RejectAsync(Guid id, Guid actor, CancellationToken ct)
    {
        var transaction = await db.Transactions.Include(x => x.Offer).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.Status != TransactionStatus.PENDING_APPROVAL)
            throw new TransactionOperationException(409, "Only pending transactions can be rejected.");
        await UpdateOfferAsync(transaction.OfferId, actor, OfferStatus.REJECTED, ct);
        await db.Entry(transaction).ReloadAsync(ct);
        return ToResponse(transaction);
    }

    private async Task UpdateOfferAsync(Guid id, Guid actor, OfferStatus status, CancellationToken ct)
    {
        var offer = await db.Offers.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new TransactionOperationException(404, "Offer was not found.");
        if (offer.Status != OfferStatus.PENDING) throw new TransactionOperationException(409, "Only pending offers can be decided.");
        var workflow = await db.AgentWorkflows.Where(x => x.MaterialMatchId == offer.MaterialMatchId &&
            x.Status == AgentWorkflowStatus.PENDING_APPROVAL).OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(ct)
            ?? throw new TransactionOperationException(409, "A validated pending workflow is required.");
        var service = new Workflows.AgentWorkflowService(db);
        try
        {
            if (status == OfferStatus.ACCEPTED) await service.ApproveAsync(workflow.Id, actor, null, ct);
            else if (status == OfferStatus.REJECTED) await service.RejectAsync(workflow.Id, actor, "Manager rejected the offer.", ct);
            else await service.ReviseAsync(workflow.Id, actor, "Manager requested an offer revision.", ct);
        }
        catch (Workflows.AgentWorkflowException ex) { throw new TransactionOperationException(ex.StatusCode, ex.Message); }
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> query, string? status, DateTimeOffset? from, DateTimeOffset? to, Guid? user)
        where T : class => query;

    private static IQueryable<Offer> Filter(IQueryable<Offer> query, string? status, DateTimeOffset? from, DateTimeOffset? to, Guid? user)
    {
        if (status is not null) query = query.Where(x => x.Status == Enum.Parse<OfferStatus>(status, true));
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.UtcDateTime);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc <= to.Value.UtcDateTime);
        if (user.HasValue) query = query.Where(x => x.BuyerId == user || x.SellerId == user);
        return query;
    }

    private static IQueryable<Transaction> Filter(IQueryable<Transaction> query, string? status, DateTimeOffset? from, DateTimeOffset? to, Guid? user)
    {
        if (status is not null) query = query.Where(x => x.Status == Enum.Parse<TransactionStatus>(status, true));
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.UtcDateTime);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc <= to.Value.UtcDateTime);
        if (user.HasValue) query = query.Where(x => x.BuyerId == user || x.SellerId == user);
        return query;
    }

    private static IOrderedQueryable<Offer> SortOffers(IQueryable<Offer> q, OfferQuery input) => input.SortBy.ToLowerInvariant() switch
    {
        "value" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.TotalValue).ThenBy(x => x.Id) : q.OrderByDescending(x => x.TotalValue).ThenBy(x => x.Id),
        "status" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.Status).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Status).ThenBy(x => x.Id),
        _ => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
    };

    private static IOrderedQueryable<Transaction> SortTransactions(IQueryable<Transaction> q, TransactionQuery input) => input.SortBy.ToLowerInvariant() switch
    {
        "value" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.TotalValue).ThenBy(x => x.Id) : q.OrderByDescending(x => x.TotalValue).ThenBy(x => x.Id),
        "status" => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.Status).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Status).ThenBy(x => x.Id),
        _ => input.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? q.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
    };

    private static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true)) throw new TransactionOperationException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
    }
    private static int Pages(int total, int size) => (int)Math.Ceiling(total / (double)size);
    private static AuditLog Log(Guid id, Guid actor, string action) => new() { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(Transaction), EntityId = id, Action = action };
    private static TransactionResponse ToResponse(Transaction x) => new(x.Id, x.OfferId, x.BuyerId, x.SellerId, x.Quantity, x.TotalValue, x.ReservedQuantity, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.CompletedAtUtc);
}
