using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MaterialListingQueryBuilderTests
{
    [Fact]
    public void Seller_search_category_and_status_are_intersected()
    {
        var tiles = Category("Tiles");
        var other = Category("Other");
        var expected = Listing("Blue tile", "", tiles, 12m, 1m, ListingStatus.ACTIVE, MaterialCondition.GOOD);
        var rows = new[] { expected,
            Listing("Blue tile", "", tiles, 12m, 1m, ListingStatus.DRAFT, MaterialCondition.GOOD),
            Listing("Blue tile", "", other, 12m, 1m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Red tile", "", tiles, 12m, 1m, ListingStatus.ACTIVE, MaterialCondition.GOOD) };
        var result = MaterialListingQueryBuilder.ApplyFilters(rows.AsQueryable(), new MaterialListingQuery {
            Search = "blue", Category = tiles.Id.ToString(), Status = "ACTIVE" });
        Assert.Equal(expected.Id, Assert.Single(result).Id);
    }

    [Fact]
    public void Search_matches_title_description_and_category()
    {
        var tiles = Category("Tiles");
        var aggregates = Category("Aggregates");
        var listings = new[]
        {
            Listing("Steel beams", "Recovered from tile demolition", aggregates, 10m, 5m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Clean concrete", "Ready for reuse", tiles, 20m, 7m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Unused timber", "Dry boards", aggregates, 30m, 9m, ListingStatus.ACTIVE, MaterialCondition.NEW)
        };

        var result = MaterialListingQueryBuilder.ApplyFilters(
                listings.AsQueryable(),
                new MaterialListingQuery { Search = "tile" })
            .Select(listing => listing.Title)
            .ToList();

        Assert.Equal(new[] { "Steel beams", "Clean concrete" }, result);
    }

    [Fact]
    public void Filters_apply_category_status_condition_and_price_range()
    {
        var tiles = Category("Tiles");
        var aggregates = Category("Aggregates");
        var expected = Listing("Good tile", "Expected", tiles, 10m, 25m, ListingStatus.ACTIVE, MaterialCondition.GOOD);
        var listings = new[]
        {
            expected,
            Listing("Draft tile", "Wrong status", tiles, 10m, 25m, ListingStatus.DRAFT, MaterialCondition.GOOD),
            Listing("Fair tile", "Wrong condition", tiles, 10m, 25m, ListingStatus.ACTIVE, MaterialCondition.FAIR),
            Listing("Expensive tile", "Wrong price", tiles, 10m, 50m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Good aggregate", "Wrong category", aggregates, 10m, 25m, ListingStatus.ACTIVE, MaterialCondition.GOOD)
        };

        var result = MaterialListingQueryBuilder.ApplyFilters(
                listings.AsQueryable(),
                new MaterialListingQuery
                {
                    Category = tiles.Id.ToString(),
                    Status = "active",
                    Condition = "good",
                    MinPrice = 20m,
                    MaxPrice = 30m
                })
            .ToList();

        Assert.Single(result);
        Assert.Equal(expected.Id, result[0].Id);
    }

    [Fact]
    public void Sorting_and_pagination_return_the_requested_page_and_correct_totals()
    {
        var category = Category("Tiles");
        var listings = new[]
        {
            Listing("Fourth", "", category, 40m, 40m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("First", "", category, 10m, 10m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Fifth", "", category, 50m, 50m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Second", "", category, 20m, 20m, ListingStatus.ACTIVE, MaterialCondition.GOOD),
            Listing("Third", "", category, 30m, 30m, ListingStatus.ACTIVE, MaterialCondition.GOOD)
        };
        var query = new MaterialListingQuery { SortBy = "unitPrice", SortDir = "asc", Page = 2, PageSize = 2 };
        var filtered = MaterialListingQueryBuilder.ApplyFilters(listings.AsQueryable(), query);
        var totalCount = filtered.Count();
        var page = MaterialListingQueryBuilder.ApplySort(filtered, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(listing => listing.Title)
            .ToList();

        Assert.Equal(5, totalCount);
        Assert.Equal(3, (int)Math.Ceiling(totalCount / (double)query.PageSize));
        Assert.Equal(new[] { "Third", "Fourth" }, page);
    }

    private static Category Category(string name) => new() { Id = Guid.NewGuid(), Name = name };

    private static Listing Listing(
        string title,
        string description,
        Category category,
        decimal quantity,
        decimal unitPrice,
        ListingStatus status,
        MaterialCondition condition) => new()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Title = title,
            Description = description,
            Quantity = quantity,
            Unit = "kg",
            UnitPrice = unitPrice,
            Status = status,
            Condition = condition,
            AvailableUntil = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        };
}