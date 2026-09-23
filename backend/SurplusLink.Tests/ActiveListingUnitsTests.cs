using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class ActiveListingUnitsTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Buyer_receives_distinct_normalized_units_from_active_listings_in_the_selected_category()
    {
        Guid categoryId;
        using (var db = fixture.Context())
        {
            categoryId = await db.Categories.Select(category => category.Id).FirstAsync();
            var otherCategory = Guid.NewGuid();
            db.Categories.Add(new Category { Id = otherCategory, Name = "Other units " + Guid.NewGuid() });
            db.Listings.AddRange(
                Listing(categoryId, " PCS ", ListingStatus.ACTIVE),
                Listing(categoryId, "pcs", ListingStatus.ACTIVE),
                Listing(categoryId, "M2", ListingStatus.ACTIVE),
                Listing(categoryId, "kg", ListingStatus.DRAFT),
                Listing(otherCategory, "bags", ListingStatus.ACTIVE));
            await db.SaveChangesAsync();
        }

        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer);
        using var response = await buyer.GetAsync($"/api/material-categories/{categoryId}/active-units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["m2", "pcs"], Assert.IsType<string[]>(await response.Content.ReadFromJsonAsync<string[]>()));
    }

    private Listing Listing(Guid categoryId, string unit, ListingStatus status) => new()
    {
        Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = categoryId,
        Title = "Units test", Description = "Listing used to select a buyer requirement unit.",
        Quantity = 10, Unit = unit, UnitPrice = 1, AvailableUntil = DateTime.UtcNow.AddDays(7),
        Status = status, Condition = MaterialCondition.GOOD
    };
}