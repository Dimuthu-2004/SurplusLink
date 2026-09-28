using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;
using Xunit;

namespace SurplusLink.Tests;

public sealed class ConstructionItemTemplateTests
{
    private static SurplusLinkDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseInMemoryDatabase(databaseName: "TemplatesTestDb_" + Guid.NewGuid())
            .Options;
        var db = new SurplusLinkDbContext(options);

        // Seed categories and templates
        db.Categories.AddRange(ConstructionItemTemplateCatalogSeed.GetCategories());
        db.ConstructionItemTemplates.AddRange(ConstructionItemTemplateCatalogSeed.GetTemplates());
        db.SaveChanges();
        return db;
    }

    [Fact]
    public void Seed_catalog_contains_all_required_categories_and_templates()
    {
        var categories = ConstructionItemTemplateCatalogSeed.GetCategories();
        Assert.True(categories.Length >= 13, "Expected at least 13 categories");

        var categoryNames = categories.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Structural & Masonry", categoryNames);
        Assert.Contains("Concrete & Aggregates", categoryNames);
        Assert.Contains("Finishes", categoryNames);
        Assert.Contains("Timber & Boards", categoryNames);
        Assert.Contains("Roofing", categoryNames);
        Assert.Contains("Plumbing", categoryNames);
        Assert.Contains("Electrical", categoryNames);
        Assert.Contains("Doors / Windows / Fixtures", categoryNames);
        Assert.Contains("Temporary Works", categoryNames);
        Assert.Contains("Construction Tools", categoryNames);
        Assert.Contains("Machinery / Equipment", categoryNames);
        Assert.Contains("Site/Safety Equipment", categoryNames);
        Assert.Contains("Miscellaneous Construction Surplus", categoryNames);

        var templates = ConstructionItemTemplateCatalogSeed.GetTemplates();
        Assert.True(templates.Length >= 30, $"Expected at least 30 templates, got {templates.Length}");

        // Verify key templates from prompt
        Assert.Contains(templates, t => t.Name == "Paint" && t.QuantityMode == "PACKAGE" && t.BaseUnit == "L" && t.PackageType == "CAN");
        Assert.Contains(templates, t => t.Name == "Generator" && t.QuantityMode == "PIECE" && t.ItemClass == "EQUIPMENT");
        Assert.Contains(templates, t => t.Name == "Reinforcement Steel" && t.QuantityMode == "PIECE");
        Assert.Contains(templates, t => t.Name == "Cement" && t.QuantityMode == "PACKAGE" && t.BaseUnit == "kg");
        Assert.Contains(templates, t => t.Name == "Tiles" && t.QuantityMode == "PACKAGE" && t.BaseUnit == "sqm");
        Assert.Contains(templates, t => t.Name == "Sand" && t.QuantityMode == "CONTINUOUS_BULK" && t.BaseUnit == "m3");
        Assert.Contains(templates, t => t.Name == "Roofing Sheets" && t.QuantityMode == "PIECE" && t.BaseUnit == "sheet");
        Assert.Contains(templates, t => t.Name == "PVC Pipes" && t.QuantityMode == "PIECE" && t.BaseUnit == "piece");
        Assert.Contains(templates, t => t.Name == "Air Compressor" && t.ItemClass == "EQUIPMENT" && t.QuantityMode == "PIECE");
        Assert.Contains(templates, t => t.Name == "Angle Grinder" && t.ItemClass == "TOOL" && t.QuantityMode == "PIECE");
        Assert.Contains(templates, t => t.Name == "Custom Construction Item" && t.ItemClass == "OTHER_CONSTRUCTION");
    }

    [Fact]
    public void All_seeded_templates_have_valid_json_attribute_schemas()
    {
        var templates = ConstructionItemTemplateCatalogSeed.GetTemplates();

        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.AttributeSchema), $"Template {template.Name} has empty schema");
            var doc = JsonDocument.Parse(template.AttributeSchema);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.True(element.TryGetProperty("id", out var idProp) && !string.IsNullOrWhiteSpace(idProp.GetString()),
                    $"Template {template.Name} field missing 'id'");
                Assert.True(element.TryGetProperty("label", out var labelProp) && !string.IsNullOrWhiteSpace(labelProp.GetString()),
                    $"Template {template.Name} field missing 'label'");
                Assert.True(element.TryGetProperty("type", out var typeProp) && !string.IsNullOrWhiteSpace(typeProp.GetString()),
                    $"Template {template.Name} field missing 'type'");
            }
        }
    }

    [Theory]
    [InlineData("5kVA diesel generator", "Generator", "EQUIPMENT", "PIECE")]
    [InlineData("paint 10L", "Paint", "MATERIAL", "PACKAGE")]
    [InlineData("12mm steel rods", "Reinforcement Steel", "MATERIAL", "PIECE")]
    [InlineData("cement 50kg bag", "Cement", "MATERIAL", "PACKAGE")]
    [InlineData("sand 5 m3", "Sand", "MATERIAL", "CONTINUOUS_BULK")]
    [InlineData("air compressor 50L", "Air Compressor", "EQUIPMENT", "PIECE")]
    [InlineData("angle grinder 100mm", "Angle Grinder", "TOOL", "PIECE")]
    public async Task Controller_resolve_endpoint_maps_phrases_to_templates(
        string phrase, string expectedTemplate, string expectedClass, string expectedMode)
    {
        using var db = CreateInMemoryDbContext();
        var controller = new ConstructionItemTemplatesController(db);

        var actionResult = await controller.ResolvePhrase(phrase);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<TemplateMatchResolutionResponse>(okResult.Value);

        Assert.Equal(expectedTemplate, result.TemplateName);
        Assert.Equal(expectedClass, result.ItemClass);
        Assert.Equal(expectedMode, result.QuantityMode);
        Assert.True(result.Confidence >= 0.8m);
    }

    [Fact]
    public async Task Controller_filters_by_search_and_category()
    {
        using var db = CreateInMemoryDbContext();
        var controller = new ConstructionItemTemplatesController(db);

        // Search for "paint"
        var searchResult = await controller.GetTemplates(search: "paint");
        var okSearch = Assert.IsType<OkObjectResult>(searchResult.Result);
        var templates = Assert.IsAssignableFrom<IReadOnlyList<ConstructionItemTemplateResponse>>(okSearch.Value);
        Assert.Contains(templates, t => t.Name == "Paint");
        Assert.DoesNotContain(templates, t => t.Name == "Generator");

        // Filter by Machinery & Equipment category
        var catResult = await controller.GetTemplates(categoryId: ConstructionItemTemplateCatalogSeed.MachineryAndEquipmentId);
        var okCat = Assert.IsType<OkObjectResult>(catResult.Result);
        var equipTemplates = Assert.IsAssignableFrom<IReadOnlyList<ConstructionItemTemplateResponse>>(okCat.Value);
        Assert.All(equipTemplates, t => Assert.Equal("Machinery / Equipment", t.CategoryName));
        Assert.Contains(equipTemplates, t => t.Name == "Generator");
        Assert.Contains(equipTemplates, t => t.Name == "Air Compressor");
    }

    [Fact]
    public async Task Controller_manager_can_create_update_and_toggle_template()
    {
        using var db = CreateInMemoryDbContext();
        var controller = new ConstructionItemTemplatesController(db);

        // Create
        var createRequest = new CreateConstructionItemTemplateRequest
        {
            Name = "Hydraulic Jack",
            CategoryId = ConstructionItemTemplateCatalogSeed.ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = "[{\"id\":\"capacityTon\",\"label\":\"Capacity (Tons)\",\"type\":\"number\"}]",
            PriceBasis = "PER_UNIT"
        };
        var createdResult = await controller.CreateTemplate(createRequest);
        var createdAt = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
        var created = Assert.IsType<ConstructionItemTemplateResponse>(createdAt.Value);
        Assert.Equal("Hydraulic Jack", created.Name);
        Assert.True(created.IsActive);

        // Toggle status
        var toggleResult = await controller.ToggleTemplateStatus(created.Id, new ToggleTemplateStatusRequest { IsActive = false });
        var okToggle = Assert.IsType<OkObjectResult>(toggleResult.Result);
        var toggled = Assert.IsType<ConstructionItemTemplateResponse>(okToggle.Value);
        Assert.False(toggled.IsActive);

        // Update
        var updateRequest = new UpdateConstructionItemTemplateRequest
        {
            Name = "Hydraulic Jack (Heavy Duty)",
            CategoryId = ConstructionItemTemplateCatalogSeed.ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = "[{\"id\":\"capacityTon\",\"label\":\"Capacity (Tons)\",\"type\":\"number\"}]",
            PriceBasis = "PER_UNIT"
        };
        var updateResult = await controller.UpdateTemplate(created.Id, updateRequest);
        var okUpdate = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updated = Assert.IsType<ConstructionItemTemplateResponse>(okUpdate.Value);
        Assert.Equal("Hydraulic Jack (Heavy Duty)", updated.Name);
    }

    [Fact]
    public void Applying_paint_template_calculates_package_quantities_without_seller_quantity_mode()
    {
        var paintTemplate = ConstructionItemTemplateCatalogSeed.GetTemplates()
            .First(t => t.Name == "Paint");

        var listing = new Listing();
        var request = new CreateMaterialListingRequest
        {
            CategoryId = paintTemplate.CategoryId,
            Title = "Dulux Weathershield Paint",
            Description = "Surplus exterior paint in unopened cans",
            // Notice: Seller does NOT supply QuantityMode or BaseUnit
            PackageSize = 4m,     // 4 Litre can
            PackageCount = 5,    // 5 cans
            UnitPrice = 4500m,
            Unit = "L",
            Condition = "NEW",
            AvailableUntil = DateTime.UtcNow.AddDays(14),
            ConstructionItemTemplateId = paintTemplate.Id,
            SpecificationsJson = "{\"colour\":\"Brilliant White\",\"finish\":\"Gloss\"}"
        };

        var method = typeof(MaterialInventoryService)
            .GetMethod("ApplyListingRequest",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                [typeof(Listing), typeof(CreateMaterialListingRequest), typeof(ConstructionItemTemplate)])
            ?? throw new InvalidOperationException("ApplyListingRequest method not found");

        method.Invoke(null, [listing, request, paintTemplate]);

        // Verifications:
        // 5 cans * 4L = 20L
        Assert.Equal(20m, listing.Quantity);
        Assert.Equal(QuantityMode.PACKAGE, listing.QuantityMode);
        Assert.Equal(4m, listing.PackageSize);
        Assert.Equal(5, listing.PackageCount);
        Assert.Equal(PackageType.CAN, listing.PackageType);
        Assert.Equal("l", listing.BaseUnit);
        Assert.Equal(paintTemplate.Id, listing.ConstructionItemTemplateId);
        Assert.Contains("Brilliant White", listing.SpecificationsJson);
    }

    [Fact]
    public void Applying_generator_template_configures_equipment_piece_without_package_controls()
    {
        var genTemplate = ConstructionItemTemplateCatalogSeed.GetTemplates()
            .First(t => t.Name == "Generator");

        var listing = new Listing();
        var request = new CreateMaterialListingRequest
        {
            CategoryId = genTemplate.CategoryId,
            Title = "Perkins 5kVA Diesel Generator",
            Description = "Reliable site generator with low running hours",
            Quantity = 1m,
            UnitPrice = 250000m,
            Unit = "piece",
            Condition = "EXCELLENT",
            AvailableUntil = DateTime.UtcNow.AddDays(30),
            ConstructionItemTemplateId = genTemplate.Id,
            SpecificationsJson = "{\"capacityKva\":5,\"fuelType\":\"Diesel\",\"runningHours\":120}"
        };

        var method = typeof(MaterialInventoryService)
            .GetMethod("ApplyListingRequest",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                [typeof(Listing), typeof(CreateMaterialListingRequest), typeof(ConstructionItemTemplate)])
            ?? throw new InvalidOperationException("ApplyListingRequest method not found");

        method.Invoke(null, [listing, request, genTemplate]);

        // Verifications:
        Assert.Equal(1m, listing.Quantity);
        Assert.Equal(QuantityMode.PIECE, listing.QuantityMode);
        Assert.Equal(1, listing.PackageCount);
        Assert.Equal(1m, listing.PackageSize);
        Assert.Equal(PackageType.PIECE, listing.PackageType);
        Assert.Equal("piece", listing.BaseUnit);
        Assert.Equal(genTemplate.Id, listing.ConstructionItemTemplateId);
        Assert.Contains("Diesel", listing.SpecificationsJson);
    }

    [Fact]
    public void Custom_construction_item_is_marked_for_manager_review()
    {
        var customTemplate = ConstructionItemTemplateCatalogSeed.GetTemplates()
            .First(t => t.Name == "Custom Construction Item");

        var listing = new Listing();
        var request = new CreateMaterialListingRequest
        {
            CategoryId = customTemplate.CategoryId,
            Title = "Custom Concrete Additive Dispenser",
            Description = "Custom fabrication tool",
            Quantity = 2m,
            UnitPrice = 35000m,
            Unit = "piece",
            Condition = "GOOD",
            AvailableUntil = DateTime.UtcNow.AddDays(10),
            ConstructionItemTemplateId = customTemplate.Id,
            IsCustomPendingReview = true,
            SpecificationsJson = "{\"soldAs\":\"Individual piece / unit\"}"
        };

        var method = typeof(MaterialInventoryService)
            .GetMethod("ApplyListingRequest",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                [typeof(Listing), typeof(CreateMaterialListingRequest), typeof(ConstructionItemTemplate)])
            ?? throw new InvalidOperationException("ApplyListingRequest method not found");

        method.Invoke(null, [listing, request, customTemplate]);

        Assert.True(listing.IsCustomPendingReview);
        Assert.Equal(2m, listing.Quantity);
        Assert.Equal(QuantityMode.PIECE, listing.QuantityMode);
    }
}
