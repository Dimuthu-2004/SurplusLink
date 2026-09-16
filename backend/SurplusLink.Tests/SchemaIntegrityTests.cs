using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class SchemaIntegrityTests
{
    [Fact]
    public void Marketplace_entities_have_primary_and_foreign_keys()
    {
        using var context = CreateContext();
        var model = GetModel(context);

        Assert.All(model.GetEntityTypes(), entity => Assert.NotNull(entity.FindPrimaryKey()));
        AssertForeignKey<Listing>(model, nameof(Listing.SellerId), typeof(User));
        AssertForeignKey<Listing>(model, nameof(Listing.CategoryId), typeof(Category));
        AssertForeignKey<BuyerRequest>(model, nameof(BuyerRequest.BuyerId), typeof(User));
        AssertForeignKey<BuyerRequest>(model, nameof(BuyerRequest.CategoryId), typeof(Category));
        AssertForeignKey<MaterialMatch>(model, nameof(MaterialMatch.ListingId), typeof(Listing));
        AssertForeignKey<MaterialMatch>(model, nameof(MaterialMatch.MaterialRequestId), typeof(BuyerRequest));
        AssertForeignKey<Workflow>(model, nameof(Workflow.MaterialMatchId), typeof(MaterialMatch));
        AssertForeignKey<Reservation>(model, nameof(Reservation.ListingId), typeof(Listing));
        AssertForeignKey<Reservation>(model, nameof(Reservation.MaterialRequestId), typeof(BuyerRequest));
        AssertForeignKey<AuditLog>(model, nameof(AuditLog.ActorUserId), typeof(User));
    }

    [Fact]
    public void Database_checks_enforce_numeric_invariants()
    {
        using var context = CreateContext();
        var constraintNames = GetModel(context).GetEntityTypes()
            .SelectMany(entity => entity.GetCheckConstraints())
            .Select(constraint => constraint.Name)
            .ToHashSet(StringComparer.Ordinal);

        var expected = new[]
        {
            "CK_Listings_Quantity_Positive",
            "CK_Listings_Price_Positive",
            "CK_Listings_ReservedQuantity_Range",
            "CK_MaterialRequests_Quantity_Positive",
            "CK_MaterialRequests_Budget_Positive",
            "CK_Matches_Score_Range",
            "CK_Reservations_Quantity_Positive"
        };

        Assert.All(expected, name => Assert.Contains(name, constraintNames));
    }

    [Fact]
    public void Required_unique_and_query_indexes_are_configured()
    {
        using var context = CreateContext();
        var indexes = GetModel(context).GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes())
            .Where(index => index.GetDatabaseName() is not null)
            .ToDictionary(index => index.GetDatabaseName()!, StringComparer.Ordinal);

        Assert.True(indexes["UX_Users_Email"].IsUnique);
        Assert.True(indexes["UX_Categories_Name"].IsUnique);
        Assert.True(indexes["UX_Matches_MaterialRequestId_ListingId"].IsUnique);
        Assert.True(indexes["UX_Workflows_MaterialMatchId"].IsUnique);

        var requiredQueryIndexes = new[]
        {
            "IX_Listings_Status",
            "IX_Listings_CategoryId",
            "IX_Listings_SellerId",
            "IX_MaterialRequests_Status",
            "IX_MaterialRequests_CategoryId",
            "IX_MaterialRequests_DeadlineUtc",
            "IX_Matches_MaterialRequestId_Score",
            "IX_Workflows_Status",
            "IX_AuditLogs_EntityType_EntityId_CreatedAtUtc"
        };

        Assert.All(requiredQueryIndexes, name => Assert.Contains(name, indexes.Keys));
    }

    [Fact]
    public void Auditable_entities_have_utc_timestamps_and_categories_are_seeded()
    {
        using var context = CreateContext();
        var model = GetModel(context);
        var auditableEntities = model.GetEntityTypes()
            .Where(entity => typeof(AuditableEntity).IsAssignableFrom(entity.ClrType));

        Assert.All(auditableEntities, entity =>
        {
            Assert.NotNull(entity.FindProperty(nameof(AuditableEntity.CreatedAtUtc)));
            Assert.NotNull(entity.FindProperty(nameof(AuditableEntity.UpdatedAtUtc)));
        });

        var category = model.FindEntityType(typeof(Category));
        Assert.NotNull(category);
        var seedNames = category.GetSeedData()
            .Select(seed => Assert.IsType<string>(seed[nameof(Category.Name)]))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(6, seedNames.Count);
        Assert.Contains("Cement", seedNames);
        Assert.Contains("Steel", seedNames);
    }

    [Fact]
    public void Listing_uses_postgresql_xmin_for_optimistic_concurrency()
    {
        using var context = CreateContext();
        var listing = GetModel(context).FindEntityType(typeof(Listing));
        Assert.NotNull(listing);
        var version = listing.FindProperty(nameof(Listing.Version));
        Assert.NotNull(version);

        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
        Assert.Equal(
            "xmin",
            version.GetColumnName(StoreObjectIdentifier.Table("Listings", schema: null)));
    }

    private static SurplusLinkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql("Host=localhost;Database=surpluslink_model_tests")
            .Options;
        return new SurplusLinkDbContext(options);
    }

    private static IModel GetModel(SurplusLinkDbContext context) =>
        context.GetService<IDesignTimeModel>().Model;

    private static void AssertForeignKey<TEntity>(IModel model, string propertyName, Type principalType)
    {
        var entity = model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Contains(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == principalType
            && foreignKey.Properties.Any(property => property.Name == propertyName));
    }
}
