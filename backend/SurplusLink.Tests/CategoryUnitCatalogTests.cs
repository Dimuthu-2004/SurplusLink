using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Requirements;

namespace SurplusLink.Tests;

public sealed class CategoryUnitCatalogTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
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
