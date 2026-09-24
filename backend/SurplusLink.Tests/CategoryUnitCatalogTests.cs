using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Requirements;

namespace SurplusLink.Tests;

public sealed class CategoryUnitCatalogTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Category_names_ignore_case_and_extra_whitespace_on_create_and_update()
    {
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        var suffix = Guid.NewGuid().ToString();
        var name = "Floor Tiles " + suffix;
        using var created = await manager.PostAsJsonAsync("/api/material-categories", new { name, allowedUnits = new[] { "pcs" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = (await created.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
        foreach (var duplicate in new[] { name.ToUpperInvariant(), "  floor   tiles  " + suffix + "  ", "floor\ttiles " + suffix })
        {
            using var response = await manager.PostAsJsonAsync("/api/material-categories", new { name = duplicate, allowedUnits = new[] { "pcs" } });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("A category with this name already exists.", await response.Content.ReadAsStringAsync());
        }
        using var otherResponse = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Other " + suffix, allowedUnits = new[] { "pcs" } });
        var other = (await otherResponse.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
        using var collision = await manager.PutAsJsonAsync($"/api/material-categories/{other.Id}", new { name = " floor   TILES " + suffix, allowedUnits = new[] { "pcs" } });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        Assert.Contains("A category with this name already exists.", await collision.Content.ReadAsStringAsync());
        using var same = await manager.PutAsJsonAsync($"/api/material-categories/{category.Id}", new { name = "  FLOOR   TILES " + suffix, allowedUnits = new[] { "pcs" } });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        using var deleted = await manager.DeleteAsync($"/api/material-categories/{other.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [PostgresFact]
    public async Task Any_listing_blocks_category_deletion_and_preserves_stock()
    {
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var created = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Protected " + Guid.NewGuid(), allowedUnits = new[] { "pcs" } });
        var category = (await created.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
        using var listingResponse = await seller.PostAsJsonAsync("/api/materials", new { categoryId = category.Id, title = "Stock", description = "Reusable stock", quantity = 12.25m, unit = "pcs", condition = "GOOD", unitPrice = 1, availableUntil = DateTime.UtcNow.AddDays(7) });
        Assert.Equal(HttpStatusCode.Created, listingResponse.StatusCode);
        var listing = (await listingResponse.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
        foreach (var status in Enum.GetValues<SurplusLink.Api.Models.ListingStatus>())
        {
            using (var db = fixture.Context())
            {
                var row = await db.Listings.SingleAsync(x => x.Id == listing.Id);
                row.Status = status;
                await db.SaveChangesAsync();
            }
            using var response = await manager.DeleteAsync($"/api/material-categories/{category.Id}");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("This category cannot be deleted because material listings are using it.", await response.Content.ReadAsStringAsync());
        }
        using var verify = fixture.Context();
        Assert.True(await verify.Categories.AnyAsync(x => x.Id == category.Id));
        Assert.Equal(12.25m, (await verify.Listings.SingleAsync(x => x.Id == listing.Id)).Quantity);
    }

    [PostgresFact]
    public async Task Manager_assignments_work_without_listings_and_enforce_canonical_units_for_both_roles()
    {
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        using var buyer = fixture.Client(app, fixture.Buyer);
        var catalog = await manager.GetFromJsonAsync<string[]>("/api/material-categories/unit-catalog");
        Assert.All(new[] { "kg", "pcs", "m", "m2", "l", "bag", "box", "set", "roll", "sheet" }, unit => Assert.Contains(unit, catalog!));
        using var created = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Catalog " + Guid.NewGuid(), allowedUnits = new[] { " KG ", "kg", "BOX" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = (await created.Content.ReadFromJsonAsync<MaterialCategoryResponse>())!;
        Assert.Equal(new[] { "box", "kg" }, category.AllowedUnits);
        Assert.Equal(new[] { "box", "kg" }, await buyer.GetFromJsonAsync<string[]>($"/api/material-categories/{category.Id}/units"));
        using var db = fixture.Context();
        Assert.False(await db.Listings.AnyAsync(row => row.CategoryId == category.Id));
        foreach (var unit in new[] { " KG ", "arbitrary" })
        {
            using var listing = await seller.PostAsJsonAsync("/api/materials", new { categoryId = category.Id, title = "Stock", description = "Reusable stock", quantity = 10, unit, condition = "GOOD", unitPrice = 1, availableUntil = DateTime.UtcNow.AddDays(7) });
            using var requirement = await buyer.PostAsJsonAsync("/api/requirements", new { categoryId = category.Id, requiredQuantity = 1, unit, maximumBudget = 100, deadline = DateTime.UtcNow.AddDays(3), latitude = 6, longitude = 79 });
            if (unit == "arbitrary")
            {
                Assert.Equal(HttpStatusCode.BadRequest, listing.StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, requirement.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Created, listing.StatusCode);
                Assert.Equal(HttpStatusCode.Created, requirement.StatusCode);
                Assert.Equal("kg", (await listing.Content.ReadFromJsonAsync<MaterialListingResponse>())!.Unit);
                Assert.Equal("kg", (await requirement.Content.ReadFromJsonAsync<RequirementResponse>())!.Unit);
            }
        }
        using var updated = await manager.PutAsJsonAsync($"/api/material-categories/{category.Id}", new { category.Name, allowedUnits = new[] { "box" } });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(new[] { "box" }, await seller.GetFromJsonAsync<string[]>($"/api/material-categories/{category.Id}/units"));
        foreach (var units in new[] { Array.Empty<string>(), new[] { "invented" } })
        {
            using var invalid = await manager.PostAsJsonAsync("/api/material-categories", new { name = "Invalid " + Guid.NewGuid(), allowedUnits = units });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
    }

    [PostgresFact]
    public async Task Migration_preserves_historical_units_and_does_not_rewrite_requests()
    {
        using var db = fixture.Context();
        var legacy = await db.BuyerRequests.SingleAsync(row => row.Id == fixture.LegacyRequest);
        var category = await db.Categories.SingleAsync(row => row.Id == legacy.CategoryId);
        Assert.Contains("kg", category.AllowedUnits);
        Assert.Contains("unit", category.AllowedUnits);
        Assert.Equal("unit", legacy.Unit);
    }
}
