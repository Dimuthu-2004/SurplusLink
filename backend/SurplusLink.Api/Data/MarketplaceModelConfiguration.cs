using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Data;

public static class MarketplaceModelConfiguration
{
    public static void ConfigureMarketplaceModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        ConfigureUser(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureListing(modelBuilder);
        ConfigureListingPhoto(modelBuilder);
        ConfigureBuyerRequest(modelBuilder);
        ConfigureMatch(modelBuilder);
        ConfigureWorkflow(modelBuilder);
        ConfigureReservation(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureTimestamps(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id).HasName("PK_Users");
            entity.Property(user => user.Email).HasColumnType("citext").HasMaxLength(320).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UX_Users_Email");
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id).HasName("PK_Categories");
            entity.Property(category => category.Name).HasColumnType("citext").HasMaxLength(120).IsRequired();
            entity.HasIndex(category => category.Name).IsUnique().HasDatabaseName("UX_Categories_Name");

            var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                SeedCategory("00000000-0000-0000-0000-000000000101", "Cement", seedTimestamp),
                SeedCategory("00000000-0000-0000-0000-000000000102", "Steel", seedTimestamp),
                SeedCategory("00000000-0000-0000-0000-000000000103", "Timber", seedTimestamp),
                SeedCategory("00000000-0000-0000-0000-000000000104", "Bricks", seedTimestamp),
                SeedCategory("00000000-0000-0000-0000-000000000105", "Aggregates", seedTimestamp),
                SeedCategory("00000000-0000-0000-0000-000000000106", "Tiles", seedTimestamp));
        });
    }

    private static void ConfigureListing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listings", table =>
            {
                table.HasCheckConstraint("CK_Listings_Quantity_Positive", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_Listings_Price_Positive", "\"UnitPrice\" > 0");
                table.HasCheckConstraint(
                    "CK_Listings_ReservedQuantity_Range",
                    "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"Quantity\"");
                table.HasCheckConstraint(
                    "CK_Listings_Coordinates_Valid",
                    "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" BETWEEN -90 AND 90 AND \"Longitude\" BETWEEN -180 AND 180)");
            });
            entity.HasKey(listing => listing.Id).HasName("PK_Listings");
            entity.Property(listing => listing.Title).HasMaxLength(200).IsRequired();
            entity.Property(listing => listing.Description).HasMaxLength(2_000).IsRequired();
            entity.Property(listing => listing.Quantity).HasPrecision(18, 3);
            entity.Property(listing => listing.ReservedQuantity).HasPrecision(18, 3);
            entity.Property(listing => listing.Unit).HasMaxLength(32).IsRequired();
            entity.Property(listing => listing.Condition).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(listing => listing.UnitPrice).HasPrecision(18, 2);
            entity.Property(listing => listing.Latitude).HasPrecision(9, 6);
            entity.Property(listing => listing.Longitude).HasPrecision(9, 6);
            entity.Property(listing => listing.AvailableUntil).HasColumnType("timestamp with time zone");
            entity.Property(listing => listing.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(listing => listing.Version).IsRowVersion();
            entity.HasIndex(listing => listing.Status).HasDatabaseName("IX_Listings_Status");
            entity.HasIndex(listing => new { listing.Status, listing.AvailableUntil })
                .HasDatabaseName("IX_Listings_Status_AvailableUntil");
            entity.HasIndex(listing => listing.CategoryId).HasDatabaseName("IX_Listings_CategoryId");
            entity.HasIndex(listing => listing.SellerId).HasDatabaseName("IX_Listings_SellerId");
            entity.HasIndex(listing => new { listing.Latitude, listing.Longitude })
                .HasDatabaseName("IX_Listings_Latitude_Longitude");
            entity.HasOne(listing => listing.Category)
                .WithMany()
                .HasForeignKey(listing => listing.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Listings_Categories_CategoryId");
            entity.HasOne(listing => listing.Seller)
                .WithMany()
                .HasForeignKey(listing => listing.SellerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Listings_Users_SellerId");
        });
    }

    private static void ConfigureListingPhoto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListingPhoto>(entity =>
        {
            entity.ToTable("ListingPhotos", table =>
                table.HasCheckConstraint("CK_ListingPhotos_SortOrder_NonNegative", "\"SortOrder\" >= 0"));
            entity.HasKey(photo => photo.Id).HasName("PK_ListingPhotos");
            entity.Property(photo => photo.PhotoUrl).HasMaxLength(2_048).IsRequired();
            entity.HasIndex(photo => new { photo.ListingId, photo.SortOrder })
                .IsUnique()
                .HasDatabaseName("UX_ListingPhotos_ListingId_SortOrder");
            entity.HasOne(photo => photo.Listing)
                .WithMany(listing => listing.Photos)
                .HasForeignKey(photo => photo.ListingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ListingPhotos_Listings_ListingId");
        });
    }

    private static void ConfigureBuyerRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuyerRequest>(entity =>
        {
            entity.ToTable("MaterialRequests", table =>
            {
                table.HasCheckConstraint("CK_MaterialRequests_Quantity_Positive", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_MaterialRequests_Budget_Positive", "\"Budget\" > 0");
                table.HasCheckConstraint("CK_MaterialRequests_Unit_NotBlank", "length(btrim(\"Unit\")) > 0");
                table.HasCheckConstraint("CK_MaterialRequests_Coordinates_Valid", "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL AND \"Latitude\" BETWEEN -90 AND 90 AND \"Longitude\" BETWEEN -180 AND 180)");
                table.HasCheckConstraint("CK_MaterialRequests_Status_Valid", "\"Status\" IN ('DRAFT', 'OPEN', 'MATCHING', 'MATCH_FOUND', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')");
            });
            entity.Property(request => request.Deadline).HasColumnName("DeadlineUtc");
            entity.Property(request => request.Unit).HasMaxLength(32).IsRequired();
            entity.Property(request => request.Latitude).HasPrecision(9, 6);
            entity.Property(request => request.Longitude).HasPrecision(9, 6);
            entity.Property(request => request.Version).IsRowVersion();
            entity.HasIndex(request => new { request.BuyerId, request.CreatedAtUtc }).HasDatabaseName("IX_MaterialRequests_BuyerId_CreatedAtUtc");
            entity.HasIndex(request => new { request.Status, request.Deadline }).HasDatabaseName("IX_MaterialRequests_Status_DeadlineUtc");
            entity.HasKey(request => request.Id).HasName("PK_MaterialRequests");
            entity.Property(request => request.Title).HasMaxLength(200).IsRequired();
            entity.Property(request => request.Notes).HasMaxLength(2000).IsRequired();
            entity.Property(request => request.RequiredQuantity).HasColumnName("Quantity").HasPrecision(18, 3);
            entity.Property(request => request.MaximumBudget).HasColumnName("Budget").HasPrecision(18, 2);
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasIndex(request => request.Status).HasDatabaseName("IX_MaterialRequests_Status");
            entity.HasIndex(request => request.CategoryId).HasDatabaseName("IX_MaterialRequests_CategoryId");
            entity.HasIndex(request => request.Deadline).HasDatabaseName("IX_MaterialRequests_DeadlineUtc");
            entity.HasOne(request => request.Category)
                .WithMany()
                .HasForeignKey(request => request.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MaterialRequests_Categories_CategoryId");
            entity.HasOne(request => request.Buyer)
                .WithMany()
                .HasForeignKey(request => request.BuyerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MaterialRequests_Users_BuyerId");
        });
    }

    private static void ConfigureMatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialMatch>(entity =>
        {
            entity.ToTable("Matches", table =>
                table.HasCheckConstraint("CK_Matches_Score_Range", "\"Score\" >= 0 AND \"Score\" <= 1"));
            entity.HasKey(match => match.Id).HasName("PK_Matches");
            entity.Property(match => match.Score).HasPrecision(5, 4);
            entity.HasIndex(match => new { match.MaterialRequestId, match.ListingId })
                .IsUnique()
                .HasDatabaseName("UX_Matches_MaterialRequestId_ListingId");
            entity.HasIndex(match => new { match.MaterialRequestId, match.Score })
                .IsDescending(false, true)
                .HasDatabaseName("IX_Matches_MaterialRequestId_Score");
            entity.HasOne(match => match.MaterialRequest)
                .WithMany()
                .HasForeignKey(match => match.MaterialRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Matches_MaterialRequests_MaterialRequestId");
            entity.HasOne(match => match.Listing)
                .WithMany()
                .HasForeignKey(match => match.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Matches_Listings_ListingId");
        });
    }

    private static void ConfigureWorkflow(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.ToTable("Workflows");
            entity.HasKey(workflow => workflow.Id).HasName("PK_Workflows");
            entity.Property(workflow => workflow.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasIndex(workflow => workflow.MaterialMatchId)
                .IsUnique()
                .HasDatabaseName("UX_Workflows_MaterialMatchId");
            entity.HasIndex(workflow => workflow.Status).HasDatabaseName("IX_Workflows_Status");
            entity.HasOne(workflow => workflow.MaterialMatch)
                .WithMany()
                .HasForeignKey(workflow => workflow.MaterialMatchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Workflows_Matches_MaterialMatchId");
        });
    }

    private static void ConfigureReservation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations", table =>
                table.HasCheckConstraint("CK_Reservations_Quantity_Positive", "\"Quantity\" > 0"));
            entity.HasKey(reservation => reservation.Id).HasName("PK_Reservations");
            entity.Property(reservation => reservation.Quantity).HasPrecision(18, 3);
            entity.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasIndex(reservation => reservation.ListingId).HasDatabaseName("IX_Reservations_ListingId");
            entity.HasIndex(reservation => reservation.MaterialRequestId)
                .HasDatabaseName("IX_Reservations_MaterialRequestId");
            entity.HasOne(reservation => reservation.Listing)
                .WithMany()
                .HasForeignKey(reservation => reservation.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reservations_Listings_ListingId");
            entity.HasOne(reservation => reservation.MaterialRequest)
                .WithMany()
                .HasForeignKey(reservation => reservation.MaterialRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reservations_MaterialRequests_MaterialRequestId");
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(audit => audit.Id).HasName("PK_AuditLogs");
            entity.Property(audit => audit.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(audit => audit.Action).HasMaxLength(120).IsRequired();
            entity.HasIndex(audit => new { audit.EntityType, audit.EntityId, audit.CreatedAtUtc })
                .HasDatabaseName("IX_AuditLogs_EntityType_EntityId_CreatedAtUtc");
            entity.HasOne(audit => audit.ActorUser)
                .WithMany()
                .HasForeignKey(audit => audit.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AuditLogs_Users_ActorUserId");
        });
    }

    private static void ConfigureTimestamps(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(AuditableEntity).IsAssignableFrom(type.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.Property(nameof(AuditableEntity.CreatedAtUtc))
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(nameof(AuditableEntity.UpdatedAtUtc))
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    private static Category SeedCategory(string id, string name, DateTime timestamp) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        CreatedAtUtc = timestamp,
        UpdatedAtUtc = timestamp
    };
}
