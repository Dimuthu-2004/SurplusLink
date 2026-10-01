using SurplusLink.Api.Data;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Tests;

public class UniversalStabilizationTests
{
    [Theory]
    [InlineData(6, 8, 4, true)]
    [InlineData(6, 4, 4, true)]
    [InlineData(6, 12, 4, false)]
    [InlineData(10, 10.08, 1.44, true)]
    [InlineData(350, 350, 50, true)]
    public void Minimal_package_fulfillment(decimal need, decimal selected, decimal increment, bool allowed) =>
        Assert.Equal(allowed, QuantitySemantics.IsMinimalFulfillment(need, [(selected, increment)]));

    [Fact]
    public void Minimality_is_checked_across_sellers_and_individual_packages()
    {
        Assert.True(QuantitySemantics.IsMinimalFulfillment(10, [(4, 4), (4, 4), (4, 4)]));
        Assert.False(QuantitySemantics.IsMinimalFulfillment(6, [(4, 4), (8, 4)]));
        Assert.False(QuantitySemantics.IsMinimalFulfillment(6, [(7, null)]));
    }

    [Fact]
    public void Catalog_identity_is_not_category_similarity()
    {
        var catalog = ConstructionItemTemplateCatalogSeed.GetTemplates();
        var generator = catalog.Single(x => x.Name == "Generator");
        var compressor = catalog.Single(x => x.Name == "Air Compressor");
        var request = new BuyerRequest { ConstructionItemTemplateId = generator.Id };
        Assert.False(ItemRelevance.Evaluate(request, new Listing { ConstructionItemTemplateId = compressor.Id }).Eligible);
        Assert.True(ItemRelevance.Evaluate(request, new Listing { ConstructionItemTemplateId = generator.Id }).Eligible);
        request.ConstructionItemTemplateId = catalog.Single(x => x.Name == "Paint").Id;
        Assert.True(ItemRelevance.Evaluate(request, new Listing { Title = "Exterior Wall Paint" }).Eligible);
        Assert.False(ItemRelevance.Evaluate(request, new Listing { Title = "Random Finishes item" }).Eligible);
        Assert.False(ItemRelevance.Evaluate(new BuyerRequest(), new Listing()).Eligible);
    }

    [Fact]
    public void Direct_candidate_eligibility_rejects_different_template_in_same_category()
    {
        var catalog = ConstructionItemTemplateCatalogSeed.GetTemplates();
        var generator = catalog.Single(x => x.Name == "Generator");
        var compressor = catalog.Single(x => x.Name == "Air Compressor");
        var request = new BuyerRequest
        {
            BuyerId = Guid.NewGuid(),
            CategoryId = generator.CategoryId,
            ConstructionItemTemplateId = generator.Id,
            Title = generator.Name,
            RequiredQuantity = 1,
            Unit = generator.BaseUnit,
            MaximumBudget = 1_000_000,
            Deadline = DateTime.UtcNow.AddDays(1)
        };
        var listing = new Listing
        {
            SellerId = Guid.NewGuid(),
            CategoryId = generator.CategoryId,
            ConstructionItemTemplateId = compressor.Id,
            Title = compressor.Name,
            Quantity = 2,
            Unit = generator.BaseUnit,
            UnitPrice = 100,
            AvailableUntil = DateTime.UtcNow.AddDays(2),
            Status = ListingStatus.ACTIVE
        };

        Assert.Equal("ITEM_MISMATCH", MatchService.EligibilityReason(request, listing));
        listing.ConstructionItemTemplateId = generator.Id;
        listing.Title = generator.Name;
        Assert.Null(MatchService.EligibilityReason(request, listing));
    }

    [Fact]
    public void Specification_and_calculated_package_sizes_are_authoritative()
    {
        var templates = ConstructionItemTemplateCatalogSeed.GetTemplates();
        Assert.Equal(300m, TemplatePackageSize.Resolve(templates.Single(x => x.Name == "Sealant"), "{\"volumeMl\":\"300ml\"}", 1));
        Assert.Equal(1.44m, TemplatePackageSize.Resolve(templates.Single(x => x.Name == "Tiles"),
            "{\"widthMm\":600,\"heightMm\":600,\"piecesPerBox\":4}", 99));
        var error = Assert.Throws<MaterialOperationException>(() =>
            TemplatePackageSize.Resolve(templates.Single(x => x.Name == "Sealant"), "{}", 1));
        Assert.Equal("specifications.volumeMl", error.Field);
        Assert.Equal("PACKAGE_SIZE_REQUIRED", error.Code);
    }

    [Fact]
    public void Multi_selection_route_matches_mobile_contract()
    {
        var method = typeof(RequirementsController).GetMethod("SelectMatches")!;
        var route = Assert.Single(method.GetCustomAttributes(typeof(HttpPostAttribute), false).Cast<HttpPostAttribute>());
        Assert.Equal("{id:guid}/select-matches", route.Template);
    }
}
