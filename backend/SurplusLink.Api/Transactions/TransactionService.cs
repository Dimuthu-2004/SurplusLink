using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Notifications;

namespace SurplusLink.Api.Transactions;

public sealed class TransactionService(SurplusLinkDbContext db, INotificationService? notifications = null,
    IOptions<TransactionConfirmationOptions>? confirmations = null)
{
    // Shared SQL projection: public display fields only, without per-offer lookups.
    internal static readonly Expression<Func<Offer, OfferResponse>> OfferProjection = x => new(
        x.Id, x.MaterialMatchId, x.BuyerId, x.SellerId, x.Quantity, x.UnitValue, x.TotalValue,
        x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.Buyer.FullName, x.Seller.FullName,
        x.Seller.BusinessName, x.MaterialMatch.Listing.Title, x.MaterialMatch.MaterialRequest.Title,
        x.MaterialMatch.Listing.Unit,
        x.MaterialMatch.Listing.Photos.OrderBy(photo => photo.SortOrder).Select(photo => photo.PhotoUrl).FirstOrDefault(),
        x.MaterialMatch.Listing.PackageType == null ? null : x.MaterialMatch.Listing.PackageType.ToString(),
        x.MaterialMatch.Listing.PackageSize, x.MaterialMatch.Listing.PackageCount);

    public async Task<OfferResponse> GetOfferAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var row = await db.Offers.AsNoTracking().Where(x => x.Id == id).Select(OfferProjection).SingleOrDefaultAsync(ct)
            ?? throw new TransactionOperationException(404, "Offer was not found.");
        if (!manager && row.BuyerId != actor && row.SellerId != actor)
            throw new TransactionOperationException(403, "This offer belongs to other participants.");
        return row;
    }

    public async Task<TransactionResponse> GetAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await AuthorizeHistoryAsync(id, actor, manager, ct);
        var row = await db.Transactions.AsNoTracking().Include(x => x.Buyer).Include(x => x.Seller).SingleAsync(x => x.Id == id, ct);
        var response = ToResponse(row);
        if ((manager || row.BuyerId == actor || row.SellerId == actor) &&
            row.Status is TransactionStatus.APPROVED or TransactionStatus.HANDED_OVER or TransactionStatus.MANAGER_REVIEW_REQUIRED or TransactionStatus.COMPLETED)
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
            .Select(OfferProjection).ToListAsync(ct);
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
                x.ReservedQuantity, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.CompletedAtUtc, null, null,
                x.ManagerApprovedAtUtc, x.ConfirmationDeadlineUtc, x.SellerHandoverConfirmedAtUtc, x.BuyerReceivedConfirmedAtUtc,
                x.ResolvedAtUtc, x.ResolutionReasonCode, x.ResolutionNote)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<TransactionHistoryPage> HistoryAsync(Guid id, TransactionQuery input, CancellationToken ct)
    {
        Validate(input);
        var transaction = await db.Transactions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Quantity, Unit = x.Offer.MaterialMatch.Listing.Unit, Item = x.Offer.MaterialMatch.Listing.Title })
            .SingleOrDefaultAsync(ct) ?? throw new TransactionOperationException(404, "Transaction was not found.");
        var query = db.AuditLogs.AsNoTracking().Where(x => x.EntityType == nameof(Transaction) && x.EntityId == id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((input.Page - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new TransactionHistoryEntry(x.Id, x.ActorUserId, x.Action, x.CreatedAtUtc, x.Note,
                null, null, null, null)).ToListAsync(ct);
        return new(items.Select(x => x with { TransactionReference = Reference(transaction.Id), ItemTitle = transaction.Item,
            Quantity = transaction.Quantity, Unit = transaction.Unit }).ToList(), total, Pages(total, input.PageSize), input.Page, input.PageSize);
    }

    public async Task<TransactionAnalyticsSummary> AnalyticsAsync(CancellationToken ct)
    {
        var pending = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.PENDING_APPROVAL, ct);
        var approved = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.APPROVED, ct);
        var rejected = await db.Transactions.CountAsync(x => x.Status == TransactionStatus.REJECTED, ct);
        // A buyer requirement is one deal even when it has several seller
        // allocation transactions. Dashboard completion counts must therefore
        // be measured at the requirement/group level.
        var completed = await db.BuyerRequests.CountAsync(x => x.Status == BuyerRequestStatus.COMPLETED, ct);
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
        try { await new Workflows.AgentWorkflowService(db, notifications, confirmations).ApproveAsync(workflow.Id, actor, null, ct); }
        catch (Workflows.AgentWorkflowException ex) { throw new TransactionOperationException(ex.StatusCode, ex.Message); }
        await db.Entry(transaction).ReloadAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> HandoverAsync(Guid id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await LoadForResolutionAsync(id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.SellerId != actor) throw new TransactionOperationException(403, "Only the seller can hand over materials.");
        if (transaction.Status != TransactionStatus.APPROVED) throw new TransactionOperationException(409, "Only approved transactions can be handed over.");
        if (await ActiveReservationAsync(transaction, ct) is null)
            throw new TransactionOperationException(409, "The approved stock reservation is no longer active.");
        transaction.Status = TransactionStatus.HANDED_OVER;
        transaction.SellerHandoverConfirmedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Log(id, actor, "SELLER_HANDOVER_CONFIRMED"));
        await NotifyAsync(transaction, NotificationTypes.TransactionHandoverConfirmed,
            "Seller confirmed handover", $"The seller confirmed handover for {Reference(transaction)}. Confirm Received after you receive the items.",
            NotificationContext.BUYER, NotificationPriority.ACTION_REQUIRED, "buyer-handover", ct, transaction.BuyerId);
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
        var transaction = await LoadForResolutionAsync(id, ct)
            ?? throw new TransactionOperationException(404, "Transaction was not found.");
        if (transaction.BuyerId != actor) throw new TransactionOperationException(403, "Only the buyer can confirm receipt.");
        if (transaction.Status != TransactionStatus.HANDED_OVER) throw new TransactionOperationException(409, "Materials must be handed over before receipt can be confirmed.");
        transaction.BuyerReceivedConfirmedAtUtc = DateTime.UtcNow;
        await CompleteLockedAsync(transaction, actor, null, "BUYER_CONFIRMED_RECEIPT", ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> ResolveCompletedAsync(Guid id, Guid managerId, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await LoadForResolutionAsync(id, ct) ?? throw new TransactionOperationException(404, "Transaction was not found.");
        EnsureManagerResolvable(transaction);
        await CompleteLockedAsync(transaction, managerId, CleanNote(note), "MANAGER_FOLLOW_UP", ct);
        transaction.ResolvedByManagerId = managerId;
        transaction.ResolvedAtUtc = DateTime.UtcNow;
        transaction.ResolutionReasonCode = "MANAGER_COMPLETED_AFTER_FOLLOW_UP";
        db.AuditLogs.Add(Log(id, managerId, "COMPLETED_BY_MANAGER_AFTER_FOLLOW_UP", transaction.ResolutionNote));
        await CloseWarningAsync(id, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> ResolveNotCompletedAsync(Guid id, Guid managerId, string note, CancellationToken ct)
    {
        var cleanNote = CleanNote(note);
        if (cleanNote is null) throw new TransactionOperationException(400, "A reason is required when marking a transaction not completed.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await LoadForResolutionAsync(id, ct) ?? throw new TransactionOperationException(404, "Transaction was not found.");
        EnsureManagerResolvable(transaction);
        await NotCompletedLockedAsync(transaction, managerId, "MANAGER_MARKED_NOT_COMPLETED", cleanNote, ct);
        transaction.ResolvedByManagerId = managerId;
        transaction.ResolvedAtUtc = DateTime.UtcNow;
        await CloseWarningAsync(id, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(transaction);
    }

    internal async Task ProcessDeadlineAsync(Guid id, DateTime now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockTransactionAsync(id, ct);
        var transaction = await LoadForResolutionAsync(id, ct);
        if (transaction is null || transaction.ConfirmationDeadlineUtc is null || transaction.ConfirmationDeadlineUtc > now ||
            transaction.Status is TransactionStatus.COMPLETED or TransactionStatus.NOT_COMPLETED) return;
        if (transaction.SellerHandoverConfirmedAtUtc is null)
        {
            if (transaction.Status != TransactionStatus.APPROVED) return;
            await NotCompletedLockedAsync(transaction, null, "CONFIRMATION_TIMEOUT_NO_HANDOVER",
                "Automatically closed because seller handover was not confirmed within the confirmation period.", ct);
            await CloseWarningAsync(id, ct);
        }
        else if (transaction.BuyerReceivedConfirmedAtUtc is null && transaction.Status == TransactionStatus.HANDED_OVER)
        {
            transaction.Status = TransactionStatus.MANAGER_REVIEW_REQUIRED;
            db.AuditLogs.Add(Log(id, null, "MANAGER_REVIEW_REQUIRED_AFTER_CONFIRMATION_DEADLINE",
                "Seller confirmed handover; stock remains reserved pending manager resolution."));
            await NotifyManagersAsync(transaction, NotificationTypes.TransactionFollowUpRequired,
                "Buyer receipt confirmation is still pending", $"Buyer receipt confirmation is still pending for {Reference(transaction)}. Review the transaction.",
                "deadline-review", ct);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<TransactionFollowUpResponse>> FollowUpsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = await db.Transactions.AsNoTracking()
            .Include(x => x.Buyer).Include(x => x.Seller)
            .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.Listing)
            .Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.MaterialRequest)
            .Where(x => x.Status == TransactionStatus.MANAGER_REVIEW_REQUIRED ||
                ((x.Status == TransactionStatus.APPROVED || x.Status == TransactionStatus.HANDED_OVER) && x.FollowUpNotifiedAtUtc != null))
            .OrderBy(x => x.ConfirmationDeadlineUtc).ToListAsync(ct);
        return rows.Select(x => new TransactionFollowUpResponse(x.Id, Reference(x), x.Offer.MaterialMatch.Listing.Title,
            x.Offer.MaterialMatch.Listing.Unit, x.Quantity, x.PackageCount, x.TotalValue, x.ManagerApprovedAtUtc,
            x.ConfirmationDeadlineUtc, x.ConfirmationDeadlineUtc is null ? 0 : (int)Math.Ceiling((x.ConfirmationDeadlineUtc.Value - now).TotalDays),
            x.Status, x.SellerHandoverConfirmedAtUtc, x.BuyerReceivedConfirmedAtUtc,
            new(x.Buyer.FullName, x.Buyer.Email, x.Buyer.PhoneNumber), new(x.Seller.FullName, x.Seller.Email, x.Seller.PhoneNumber))).ToList();
    }

    private async Task CompleteLockedAsync(Transaction transaction, Guid actor, string? note, string reason, CancellationToken ct)
    {
        transaction.Status = TransactionStatus.COMPLETED;
        transaction.CompletedAtUtc = DateTime.UtcNow;
        transaction.ResolutionNote = note;
        transaction.ResolutionReasonCode = reason;
        await FinalizeReservationAsync(transaction, actor, ct);
        db.AuditLogs.Add(Log(transaction.Id, actor, reason == "MANAGER_FOLLOW_UP" ? "COMPLETED_BY_MANAGER" : "BUYER_RECEIPT_CONFIRMED_TRANSACTION_COMPLETED", note));
        await CompleteRequirementIfAllCompletedAsync(transaction, actor, ct);
        await NotifyParticipantsAsync(transaction, NotificationTypes.TransactionCompleted, "Transaction completed",
            $"Transaction {Reference(transaction)} is complete.", "completed", ct);
    }

    private async Task NotCompletedLockedAsync(Transaction transaction, Guid? actor, string reason, string note, CancellationToken ct)
    {
        transaction.Status = TransactionStatus.NOT_COMPLETED;
        transaction.ResolvedAtUtc = DateTime.UtcNow;
        transaction.ResolutionReasonCode = reason;
        transaction.ResolutionNote = note;
        await ReleaseReservationAsync(transaction, actor, ct);
        db.AuditLogs.Add(Log(transaction.Id, actor, reason == "CONFIRMATION_TIMEOUT_NO_HANDOVER"
            ? "AUTOMATICALLY_CLOSED_AFTER_SELLER_CONFIRMATION_TIMEOUT" : "MARKED_NOT_COMPLETED_BY_MANAGER", note));
        var timedOut = reason == "CONFIRMATION_TIMEOUT_NO_HANDOVER";
        await NotifyParticipantsAsync(transaction, timedOut ? NotificationTypes.TransactionTimedOut : NotificationTypes.TransactionResolvedByManager,
            timedOut ? "Transaction closed after confirmation timeout" : "Transaction marked not completed",
            timedOut
                ? $"Transaction {Reference(transaction)} was closed because handover was not confirmed within the confirmation period. The reserved stock is available again."
                : $"Transaction {Reference(transaction)} was marked not completed. The reserved stock is available again.",
            timedOut ? "timeout" : "manager-not-completed", ct);
    }

    private async Task<Reservation?> ActiveReservationAsync(Transaction transaction, CancellationToken ct)
    {
        var exact = await db.Reservations.Include(x => x.Listing).SingleOrDefaultAsync(x => x.TransactionId == transaction.Id && x.Status == ReservationStatus.ACTIVE, ct);
        if (exact is not null) return exact;
        // Compatibility for existing rows created before TransactionId existed.
        var match = transaction.Offer.MaterialMatch;
        return await db.Reservations.Include(x => x.Listing).SingleOrDefaultAsync(x => x.TransactionId == null &&
            x.MaterialRequestId == match.MaterialRequestId && x.ListingId == match.ListingId && x.Status == ReservationStatus.ACTIVE, ct);
    }

    private async Task FinalizeReservationAsync(Transaction transaction, Guid? actor, CancellationToken ct)
    {
        var reservation = await ActiveReservationAsync(transaction, ct) ?? throw new TransactionOperationException(409, "The stock reservation is no longer active.");
        await LockListingAsync(reservation.ListingId, ct);
        await db.Entry(reservation.Listing).ReloadAsync(ct);
        if (reservation.Listing.ReservedQuantity < reservation.Quantity)
            throw new TransactionOperationException(409, "Reserved inventory is inconsistent and cannot be finalized.");
        reservation.Status = ReservationStatus.CONFIRMED;
        reservation.Listing.Quantity -= reservation.Quantity;
        reservation.Listing.ReservedQuantity -= reservation.Quantity;
        if (reservation.PackageCount is int packageCount)
        {
            if (reservation.Listing.PackageCount is null || reservation.Listing.ReservedPackageCount < packageCount)
                throw new TransactionOperationException(409, "Reserved physical package inventory is inconsistent.");
            reservation.Listing.PackageCount -= packageCount;
            reservation.Listing.ReservedPackageCount -= packageCount;
        }
        reservation.Listing.Status = reservation.Listing.Quantity == 0 ? ListingStatus.SOLD : ListingStatus.ACTIVE;
        transaction.ReservedQuantity = 0;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(Listing),
            EntityId = reservation.ListingId, Action = "RESERVATION_FINALIZED_STOCK_CONSUMED" });
    }

    private async Task ReleaseReservationAsync(Transaction transaction, Guid? actor, CancellationToken ct)
    {
        var reservation = await ActiveReservationAsync(transaction, ct);
        if (reservation is null) return; // already resolved by a concurrently committed terminal action
        await LockListingAsync(reservation.ListingId, ct);
        await db.Entry(reservation.Listing).ReloadAsync(ct);
        if (reservation.Listing.ReservedQuantity < reservation.Quantity)
            throw new TransactionOperationException(409, "Reserved inventory is inconsistent and cannot be released.");
        reservation.Status = ReservationStatus.RELEASED;
        reservation.Listing.ReservedQuantity -= reservation.Quantity;
        if (reservation.PackageCount is int packageCount)
        {
            if (reservation.Listing.ReservedPackageCount < packageCount)
                throw new TransactionOperationException(409, "Reserved physical package inventory is inconsistent.");
            reservation.Listing.ReservedPackageCount -= packageCount;
        }
        if (reservation.Listing.Status == ListingStatus.RESERVED) reservation.Listing.Status = ListingStatus.ACTIVE;
        transaction.ReservedQuantity = 0;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(Listing),
            EntityId = reservation.ListingId, Action = "RESERVATION_RELEASED_STOCK_RETURNED" });
    }

    private async Task CompleteRequirementIfAllCompletedAsync(Transaction transaction, Guid actor, CancellationToken ct)
    {
        var request = transaction.Offer.MaterialMatch.MaterialRequest;
        var states = await db.Transactions.Where(x => x.Offer.MaterialMatch.MaterialRequestId == request.Id)
            .Select(x => x.Status).ToListAsync(ct);
        if (states.Count == 0 || states.Any(x => x != TransactionStatus.COMPLETED)) return;
        request.Status = BuyerRequestStatus.COMPLETED;
        var workflows = await db.AgentWorkflows.Where(x => x.MaterialRequestId == request.Id && x.Status == AgentWorkflowStatus.APPROVED).ToListAsync(ct);
        foreach (var workflow in workflows)
        {
            workflow.Status = AgentWorkflowStatus.COMPLETED;
            workflow.CurrentStage = "COMPLETED";
            workflow.CompletedAtUtc = DateTime.UtcNow;
            db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(AgentWorkflow), EntityId = workflow.Id, Action = "WORKFLOW_COMPLETED" });
        }
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(BuyerRequest), EntityId = request.Id, Action = "ALL_TRANSACTIONS_COMPLETED" });
    }

    private async Task NotifyParticipantsAsync(Transaction transaction, string type, string title, string message, string suffix, CancellationToken ct)
    {
        await NotifyAsync(transaction, type, title, message, NotificationContext.BUYER, NotificationPriority.INFO, suffix + ":buyer", ct, transaction.BuyerId);
        await NotifyAsync(transaction, type, title, message, NotificationContext.SELLER, NotificationPriority.INFO, suffix + ":seller", ct, transaction.SellerId);
        await NotifyManagersAsync(transaction, type, title, message, suffix + ":manager", ct);
    }

    private async Task NotifyManagersAsync(Transaction transaction, string type, string title, string message, string suffix, CancellationToken ct)
    {
        if (notifications is null) return;
        var managers = await db.Set<UserRoleAssignment>().AsNoTracking().Where(x => x.Role == UserRole.MANAGER).Select(x => x.UserId).ToListAsync(ct);
        foreach (var managerId in managers)
            await NotifyAsync(transaction, type, title, message, NotificationContext.MANAGER, NotificationPriority.ACTION_REQUIRED, suffix, ct, managerId);
    }

    private Task NotifyAsync(Transaction transaction, string type, string title, string message, NotificationContext context,
        NotificationPriority priority, string suffix, CancellationToken ct, Guid userId) => notifications is null ? Task.CompletedTask :
        notifications.CreateAsync(new(userId, type, title, message, context, priority, nameof(Transaction), transaction.Id,
            $"/app/manager/transactions/{transaction.Id}", $"transaction:{transaction.Id}:{suffix}"), ct);

    private Task CloseWarningAsync(Guid id, CancellationToken ct) => db.Notifications
        .Where(x => x.EntityType == nameof(Transaction) && x.EntityId == id && x.Type == NotificationTypes.TransactionFollowUpRequired && !x.IsRead)
        .ExecuteUpdateAsync(x => x.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAtUtc, DateTime.UtcNow), ct);

    private static void EnsureManagerResolvable(Transaction transaction)
    {
        if (transaction.Status is not (TransactionStatus.APPROVED or TransactionStatus.HANDED_OVER or TransactionStatus.MANAGER_REVIEW_REQUIRED))
            throw new TransactionOperationException(409, "Only unresolved approved transactions can be manually resolved.");
    }

    private Task<Transaction?> LoadForResolutionAsync(Guid id, CancellationToken ct) => db.Transactions
        .Include(x => x.Buyer).Include(x => x.Seller).Include(x => x.Offer).ThenInclude(x => x.MaterialMatch)
        .ThenInclude(x => x.MaterialRequest).Include(x => x.Offer).ThenInclude(x => x.MaterialMatch).ThenInclude(x => x.Listing)
        .SingleOrDefaultAsync(x => x.Id == id, ct);

    private Task<int> LockListingAsync(Guid id, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Listings\" WHERE \"Id\" = {id} FOR UPDATE", ct);

    private static string Reference(Transaction transaction) => "TX-" + transaction.Id.ToString("N")[..8].ToUpperInvariant();
    private static string Reference(Guid id) => "TX-" + id.ToString("N")[..8].ToUpperInvariant();
    private static string? CleanNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim().Length <= 500 ? note.Trim() : throw new TransactionOperationException(400, "Resolution note must be at most 500 characters.");

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
        var service = new Workflows.AgentWorkflowService(db, notifications, confirmations);
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
    private static AuditLog Log(Guid id, Guid? actor, string action, string? note = null) => new() { Id = Guid.NewGuid(), ActorUserId = actor, EntityType = nameof(Transaction), EntityId = id, Action = action, Note = note };
    private static TransactionResponse ToResponse(Transaction x) => new(x.Id, x.OfferId, x.BuyerId, x.SellerId, x.Quantity, x.TotalValue, x.ReservedQuantity, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, x.CompletedAtUtc,
        null, null, x.ManagerApprovedAtUtc, x.ConfirmationDeadlineUtc, x.SellerHandoverConfirmedAtUtc, x.BuyerReceivedConfirmedAtUtc,
        x.ResolvedAtUtc, x.ResolutionReasonCode, x.ResolutionNote);
}
