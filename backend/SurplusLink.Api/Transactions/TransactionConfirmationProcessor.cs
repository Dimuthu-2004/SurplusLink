using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Notifications;

namespace SurplusLink.Api.Transactions;

/// <summary>Durable server-side follow-up and deadline processing.</summary>
public sealed class TransactionConfirmationProcessor(SurplusLinkDbContext db, TransactionService transactions,
    INotificationService notifications, IOptions<TransactionConfirmationOptions> options)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var configuration = options.Value;
        var followUpIds = await db.Transactions.AsNoTracking()
            .Where(x => (x.Status == TransactionStatus.APPROVED || x.Status == TransactionStatus.HANDED_OVER) &&
                x.ManagerApprovedAtUtc != null && x.FollowUpNotifiedAtUtc == null &&
                x.ManagerApprovedAtUtc <= now.AddDays(-configuration.FollowUpAfterDays))
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var id in followUpIds) await CreateFollowUpOnceAsync(id, now, ct);

        var deadlineIds = await db.Transactions.AsNoTracking()
            .Where(x => (x.Status == TransactionStatus.APPROVED || x.Status == TransactionStatus.HANDED_OVER) &&
                x.ConfirmationDeadlineUtc != null && x.ConfirmationDeadlineUtc <= now)
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var id in deadlineIds) await transactions.ProcessDeadlineAsync(id, now, ct);
    }

    private async Task CreateFollowUpOnceAsync(Guid id, DateTime now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Transactions\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var row = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is null || row.FollowUpNotifiedAtUtc is not null || row.Status is not (TransactionStatus.APPROVED or TransactionStatus.HANDED_OVER)) return;
        row.FollowUpNotifiedAtUtc = now;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(Transaction), EntityId = id,
            Action = "CONFIRMATION_FOLLOW_UP_REQUIRED" });
        var managers = await db.Set<UserRoleAssignment>().AsNoTracking().Where(x => x.Role == UserRole.MANAGER).Select(x => x.UserId).ToListAsync(ct);
        var reference = "TX-" + id.ToString("N")[..8].ToUpperInvariant();
        var message = row.SellerHandoverConfirmedAtUtc is null
            ? $"Transaction {reference} still needs seller handover confirmation."
            : $"Buyer receipt confirmation is still pending for {reference}.";
        foreach (var managerId in managers)
            await notifications.CreateAsync(new(managerId, NotificationTypes.TransactionFollowUpRequired,
                "Transaction confirmation pending", message, NotificationContext.MANAGER, NotificationPriority.ACTION_REQUIRED,
                nameof(Transaction), id, $"/app/manager/transactions/{id}", $"transaction:{id}:follow-up:manager"), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}

public sealed class TransactionConfirmationWorker(IServiceScopeFactory scopes,
    IOptions<TransactionConfirmationOptions> options, ILogger<TransactionConfirmationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Transaction confirmation worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<TransactionConfirmationProcessor>().ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning(error, "Transaction confirmation processing failed; retrying on the next poll."); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
