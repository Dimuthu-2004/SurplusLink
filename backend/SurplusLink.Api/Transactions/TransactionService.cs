using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Transactions;

public sealed class TransactionService(SurplusLinkDbContext db)
{
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
        query = Filter(query, input.Status, input.CreatedFrom, input.CreatedTo, input.UserId);
        var total = await query.CountAsync(ct);
        var ordered = SortTransactions(query, input);
        var items = await ordered.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new TransactionResponse(x.Id, x.OfferId, x.BuyerId, x.SellerId, x.Quantity, x.TotalValue,
                x.ReservedQuantity, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.CompletedAtUtc)).ToListAsync(ct);
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
        var totalDecisions = approved + rejected + completed;
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
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var transaction = await db.Transactions.Include(x => x.Offer).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.Status != TransactionStatus.PENDING_APPROVAL) throw new TransactionOperationException(409, "Only pending transactions can be approved.");
        if (transaction.BuyerId == transaction.SellerId) throw new TransactionOperationException(409, "Self-dealing transactions are not allowed.");
        var listing = await db.Matches.Where(x => x.Id == transaction.Offer.MaterialMatchId).Select(x => x.Listing).SingleAsync(ct);
        if (listing.ReservedQuantity + transaction.Quantity > listing.Quantity) throw new TransactionOperationException(409, "Insufficient available quantity.");
        listing.ReservedQuantity += transaction.Quantity;
        transaction.ReservedQuantity = transaction.Quantity;
        transaction.Status = TransactionStatus.APPROVED;
        db.AuditLogs.Add(Log(id, actor, "TRANSACTION_APPROVED"));
        db.AuditLogs.Add(Log(id, actor, "RESERVATION_CREATED"));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> CompleteAsync(Guid id, Guid actor, CancellationToken ct)
    {
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.Status != TransactionStatus.APPROVED) throw new TransactionOperationException(409, "Only approved transactions can be completed.");
        transaction.Status = TransactionStatus.COMPLETED;
        transaction.CompletedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Log(id, actor, "TRANSACTION_COMPLETED"));
        await db.SaveChangesAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> RejectAsync(Guid id, Guid actor, CancellationToken ct)
    {
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.Status != TransactionStatus.PENDING_APPROVAL)
            throw new TransactionOperationException(409, "Only pending transactions can be rejected.");
        transaction.Status = TransactionStatus.REJECTED;
        db.AuditLogs.Add(Log(id, actor, "TRANSACTION_REJECTED"));
        await db.SaveChangesAsync(ct);
        return ToResponse(transaction);
    }

    private async Task UpdateOfferAsync(Guid id, Guid actor, OfferStatus status, CancellationToken ct)
    {
        var offer = await db.Offers.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new TransactionOperationException(404, "Offer was not found.");
        offer.Status = status;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(Offer), EntityId = id,
            Action = status == OfferStatus.REVISION_REQUESTED ? "OFFER_REVISION_REQUESTED" : $"OFFER_{status}" });
        await db.SaveChangesAsync(ct);
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
