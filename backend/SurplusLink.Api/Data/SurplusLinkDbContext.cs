using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Data;

public sealed class SurplusLinkDbContext(DbContextOptions<SurplusLinkDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();

    public DbSet<BuyerRequest> BuyerRequests => Set<BuyerRequest>();

    public DbSet<MaterialMatch> Matches => Set<MaterialMatch>();

    public DbSet<Workflow> Workflows => Set<Workflow>();

    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();

    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();

    public DbSet<AgentToolCall> AgentToolCalls => Set<AgentToolCall>();

    public DbSet<Approval> Approvals => Set<Approval>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AuditRequirementStatusChanges();
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AuditRequirementStatusChanges();
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureMarketplaceModel();
    }

    private void AuditRequirementStatusChanges()
    {
        // Covers future workflow-driven transitions using tracked entities as well as buyer actions.
        // Bulk SQL/ExecuteUpdate bypasses SaveChanges and must supply its own audit records.
        foreach (var entry in ChangeTracker.Entries<BuyerRequest>()
                     .Where(x => x.State == EntityState.Modified).ToArray())
        {
            var status = entry.Property(x => x.Status);
            if (!status.IsModified || status.OriginalValue == status.CurrentValue) continue;
            var action = $"STATUS_CHANGED:{status.OriginalValue}:{status.CurrentValue}";
            var pendingLogs = ChangeTracker.Entries<AuditLog>()
                .Where(x => x.State == EntityState.Added && x.Entity.EntityType == nameof(BuyerRequest) &&
                            x.Entity.EntityId == entry.Entity.Id).Select(x => x.Entity).ToArray();
            // A failed SaveChanges can be retried within the same context.
            if (pendingLogs.Any(x => x.Action == action)) continue;
            AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), EntityType = nameof(BuyerRequest), EntityId = entry.Entity.Id,
                Action = action,
                // Reuse the explicit operation's actor; background changes have no inferred user.
                ActorUserId = pendingLogs.LastOrDefault(x => x.ActorUserId.HasValue)?.ActorUserId
            });
        }
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }

                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
