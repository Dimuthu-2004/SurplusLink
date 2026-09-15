using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MaterialInventorySchemaTests
{
    [Fact]
    public void Material_listing_fields_photos_constraints_and_indexes_are_configured()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var listing = model.FindEntityType(typeof(Listing));
        Assert.NotNull(listing);

        Assert.NotNull(listing.FindProperty(nameof(Listing.Description)));
        Assert.NotNull(listing.FindProperty(nameof(Listing.Unit)));
        Assert.NotNull(listing.FindProperty(nameof(Listing.Condition)));
        Assert.NotNull(listing.FindProperty(nameof(Listing.Latitude)));
        Assert.NotNull(listing.FindProperty(nameof(Listing.Longitude)));
        Assert.NotNull(listing.FindProperty(nameof(Listing.AvailableUntil)));

        var constraints = listing.GetCheckConstraints().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("CK_Listings_Coordinates_Valid", constraints);

        var listingIndexes = listing.GetIndexes().Select(item => item.GetDatabaseName()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("IX_Listings_Status_AvailableUntil", listingIndexes);
        Assert.Contains("IX_Listings_Latitude_Longitude", listingIndexes);

        var photo = model.FindEntityType(typeof(ListingPhoto));
        Assert.NotNull(photo);
        Assert.NotNull(photo.FindPrimaryKey());
        Assert.Contains(photo.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Listing)
            && foreignKey.Properties.Single().Name == nameof(ListingPhoto.ListingId));
        Assert.Contains("CK_ListingPhotos_SortOrder_NonNegative", photo.GetCheckConstraints().Select(item => item.Name));
        Assert.Contains("UX_ListingPhotos_ListingId_SortOrder", photo.GetIndexes().Select(item => item.GetDatabaseName()));
    }

    private static SurplusLinkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql("Host=localhost;Database=surpluslink_material_model_tests")
            .Options;
        return new SurplusLinkDbContext(options);
    }
}

