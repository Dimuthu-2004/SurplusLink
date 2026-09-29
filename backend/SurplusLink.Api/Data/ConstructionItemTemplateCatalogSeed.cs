using System.Text.Json;
using System.Text.Json.Nodes;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Data;

public static class ConstructionItemTemplateCatalogSeed
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Standard Category Guids
    public static readonly Guid StructuralAndMasonryId = new("00000000-0000-0000-0000-000000000110");
    public static readonly Guid ConcreteAndAggregatesId = new("00000000-0000-0000-0000-000000000111");
    public static readonly Guid FinishesId = new("00000000-0000-0000-0000-000000000112");
    public static readonly Guid TimberAndBoardsId = new("00000000-0000-0000-0000-000000000113");
    public static readonly Guid RoofingId = new("00000000-0000-0000-0000-000000000114");
    public static readonly Guid PlumbingId = new("00000000-0000-0000-0000-000000000115");
    public static readonly Guid ElectricalId = new("00000000-0000-0000-0000-000000000116");
    public static readonly Guid DoorsWindowsFixturesId = new("00000000-0000-0000-0000-000000000117");
    public static readonly Guid TemporaryWorksId = new("00000000-0000-0000-0000-000000000118");
    public static readonly Guid ConstructionToolsId = new("00000000-0000-0000-0000-000000000119");
    public static readonly Guid MachineryAndEquipmentId = new("00000000-0000-0000-0000-00000000011A");
    public static readonly Guid SiteSafetyEquipmentId = new("00000000-0000-0000-0000-00000000011B");
    public static readonly Guid MiscSurplusId = new("00000000-0000-0000-0000-00000000011C");

    // Legacy Seed Category Guids (preserved for backward compatibility)
    public static readonly Guid LegacyCementId = new("00000000-0000-0000-0000-000000000101");
    public static readonly Guid LegacySteelId = new("00000000-0000-0000-0000-000000000102");
    public static readonly Guid LegacyTimberId = new("00000000-0000-0000-0000-000000000103");
    public static readonly Guid LegacyBricksId = new("00000000-0000-0000-0000-000000000104");
    public static readonly Guid LegacyAggregatesId = new("00000000-0000-0000-0000-000000000105");
    public static readonly Guid LegacyTilesId = new("00000000-0000-0000-0000-000000000106");

    public static Category[] GetCategories() =>
    [
        new() { Id = LegacyCementId, Name = "Cement", AllowedUnits = ["kg", "bag", "ton", "piece", "unit"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = LegacySteelId, Name = "Steel", AllowedUnits = ["piece", "rod", "kg", "ton", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = LegacyTimberId, Name = "Timber", AllowedUnits = ["piece", "board", "sqm", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = LegacyBricksId, Name = "Bricks", AllowedUnits = ["piece", "block", "sqm"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = LegacyAggregatesId, Name = "Aggregates", AllowedUnits = ["m3", "ton", "cube", "kg"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = LegacyTilesId, Name = "Tiles", AllowedUnits = ["sqm", "box", "piece"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },

        new() { Id = StructuralAndMasonryId, Name = "Structural & Masonry", AllowedUnits = ["piece", "block", "rod", "kg", "ton", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = ConcreteAndAggregatesId, Name = "Concrete & Aggregates", AllowedUnits = ["kg", "bag", "ton", "m3", "cube"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = FinishesId, Name = "Finishes", AllowedUnits = ["L", "can", "sqm", "box", "kg", "bag", "cartridge", "piece"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = TimberAndBoardsId, Name = "Timber & Boards", AllowedUnits = ["piece", "sheet", "m", "sqm"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = RoofingId, Name = "Roofing", AllowedUnits = ["sheet", "piece", "sqm", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = PlumbingId, Name = "Plumbing", AllowedUnits = ["piece", "pipe", "length", "pack", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = ElectricalId, Name = "Electrical", AllowedUnits = ["piece", "roll", "pack", "m"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = DoorsWindowsFixturesId, Name = "Doors / Windows / Fixtures", AllowedUnits = ["piece", "set", "sheet"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = TemporaryWorksId, Name = "Temporary Works", AllowedUnits = ["piece", "set", "sheet", "unit"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = ConstructionToolsId, Name = "Construction Tools", AllowedUnits = ["piece", "unit", "set"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = MachineryAndEquipmentId, Name = "Machinery / Equipment", AllowedUnits = ["piece", "unit"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = SiteSafetyEquipmentId, Name = "Site/Safety Equipment", AllowedUnits = ["piece", "set", "pack"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp },
        new() { Id = MiscSurplusId, Name = "Miscellaneous Construction Surplus", AllowedUnits = ["piece", "unit", "kg", "m", "sqm", "m3", "bag", "box", "can"], CreatedAtUtc = SeedTimestamp, UpdatedAtUtc = SeedTimestamp }
    ];

    public static ConstructionItemTemplate[] GetTemplates() => AddSriLankanFormMetadata(
    [
        // 1. Paint (Finishes)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000201"),
            Name = "Paint",
            CategoryId = FinishesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "L",
            PackageType = "CAN",
            AllowedUnits = ["L", "can"],
            AllowedPackageSizes = [1m, 4m, 10m, 20m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand / Manufacturer", type = "string", required = false, placeholder = "e.g. Dulux, Asian Paints, Nippon" },
                new { id = "paintType", label = "Paint Type", type = "select", required = true, options = new[] { "Emulsion", "Gloss / Enamel", "Weather-shield / Exterior", "Primer", "Undercoat", "Epoxy" } },
                new { id = "colour", label = "Colour", type = "string", required = true, placeholder = "e.g. Brilliant White, Cream, Slate Grey" },
                new { id = "finish", label = "Finish", type = "select", required = false, options = new[] { "Matt", "Satin", "Semi-Gloss", "High Gloss", "Eggshell" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 2. Generator (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000202"),
            Name = "Generator",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand / Manufacturer", type = "string", required = true, placeholder = "e.g. Perkins, Cummins, Honda, Denyo" },
                new { id = "model", label = "Model", type = "string", required = false, placeholder = "e.g. DG6500, EU22i" },
                new { id = "capacityKva", label = "Capacity (kVA)", type = "number", required = true, placeholder = "e.g. 5, 10, 50, 100" },
                new { id = "fuelType", label = "Fuel Type", type = "select", required = true, options = new[] { "Diesel", "Petrol", "Gas", "Dual Fuel" } },
                new { id = "phase", label = "Phase", type = "select", required = true, options = new[] { "Single Phase", "Three Phase" } },
                new { id = "voltage", label = "Voltage", type = "select", required = false, options = new[] { "230V", "400V", "110V/230V Dual" } },
                new { id = "runningHours", label = "Running Hours", type = "number", required = false, placeholder = "e.g. 450" }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 3. Cement (Concrete & Aggregates)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000203"),
            Name = "Cement",
            CategoryId = ConcreteAndAggregatesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "kg",
            PackageType = "BAG",
            AllowedUnits = ["kg", "bag"],
            AllowedPackageSizes = [50m, 25m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand / Manufacturer", type = "string", required = false, placeholder = "e.g. Tokyo Super, Insee Sanstha, Ultratech" },
                new { id = "cementType", label = "Cement Type", type = "select", required = true, options = new[] { "Ordinary Portland Cement (OPC)", "Portland Pozzolana Cement (PPC)", "Rapid Hardening", "White Cement", "Masonry Cement" } },
                new { id = "grade", label = "Grade", type = "select", required = false, options = new[] { "Grade 43", "Grade 53", "Blended / Standard" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 4. Reinforcement Steel (Structural & Masonry)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000204"),
            Name = "Reinforcement Steel",
            CategoryId = StructuralAndMasonryId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "ROD",
            AllowedUnits = ["piece", "rod", "kg", "ton"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "diameterMm", label = "Diameter (mm)", type = "select", required = true, options = new[] { "6mm", "8mm", "10mm", "12mm", "16mm", "20mm", "25mm", "32mm" } },
                new { id = "lengthM", label = "Length per rod (m)", type = "number", required = true, placeholder = "e.g. 6 or 12" },
                new { id = "grade", label = "Steel Grade", type = "select", required = true, options = new[] { "RB500 / Grade 500", "RB415 / Grade 415", "Mild Steel Grade 250" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 5. Structural Steel (Structural & Masonry)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000205"),
            Name = "Structural Steel",
            CategoryId = StructuralAndMasonryId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "length", "ton"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "sectionType", label = "Section Type", type = "select", required = true, options = new[] { "I-Beam / Universal Beam", "H-Column", "C-Channel", "Equal Angle (L-section)", "Box Section / Hollow Tube (SHS/RHS)", "Flat Bar" } },
                new { id = "dimensions", label = "Dimensions / Spec", type = "string", required = true, placeholder = "e.g. 150x75mm, 100x50x4mm" },
                new { id = "lengthM", label = "Length (m)", type = "number", required = true, placeholder = "e.g. 6.0" },
                new { id = "grade", label = "Steel Grade", type = "select", required = false, options = new[] { "S275", "S355", "Grade 43" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 6. Bricks (Structural & Masonry)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000206"),
            Name = "Bricks",
            CategoryId = StructuralAndMasonryId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brickType", label = "Brick Type", type = "select", required = true, options = new[] { "Red Clay Wire-Cut", "Traditional Burnt Clay", "Fly Ash", "Engineering Brick", "Refractory / Fire Brick" } },
                new { id = "dimensionsMm", label = "Dimensions (mm)", type = "string", required = false, placeholder = "e.g. 215 x 102.5 x 65" },
                new { id = "compressiveStrength", label = "Compressive Strength (N/mm\u00B2)", type = "string", required = false }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 7. Blocks (Structural & Masonry)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000207"),
            Name = "Blocks",
            CategoryId = StructuralAndMasonryId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "block"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "blockType", label = "Block Type", type = "select", required = true, options = new[] { "Cement Solid Block", "Cement Hollow Block", "Autoclaved Aerated Concrete (AAC)", "Cellular Lightweight Concrete (CLC)" } },
                new { id = "thicknessInches", label = "Thickness", type = "select", required = true, options = new[] { "4 inch", "6 inch", "8 inch", "9 inch" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 8. Sand (Concrete & Aggregates)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000208"),
            Name = "Sand",
            CategoryId = ConcreteAndAggregatesId,
            ItemClass = "MATERIAL",
            QuantityMode = "CONTINUOUS_BULK",
            BaseUnit = "m3",
            PackageType = null,
            AllowedUnits = ["m3", "cube", "ton"],
            AllowedPackageSizes = [],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "sandType", label = "Sand Type", type = "select", required = true, options = new[] { "River Sand", "Manufactured Sand (M-Sand)", "Plastering Sand (P-Sand)", "Filling Sand" } },
                new { id = "screeningStatus", label = "Screening", type = "select", required = false, options = new[] { "Screened / Washed", "Unscreened" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 9. Aggregate (Concrete & Aggregates)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000209"),
            Name = "Aggregate",
            CategoryId = ConcreteAndAggregatesId,
            ItemClass = "MATERIAL",
            QuantityMode = "CONTINUOUS_BULK",
            BaseUnit = "m3",
            PackageType = null,
            AllowedUnits = ["m3", "cube", "ton"],
            AllowedPackageSizes = [],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "aggregateSize", label = "Aggregate Size", type = "select", required = true, options = new[] { "10mm (3/8\")", "20mm (3/4\")", "40mm (1.5\")", "ABC (Aggregate Base Coarse)", "Quarry Dust" } },
                new { id = "aggregateType", label = "Source Type", type = "select", required = false, options = new[] { "Crushed Granite Rock", "Limestone", "Gravel Aggregate" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 10. Gravel (Concrete & Aggregates)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020A"),
            Name = "Gravel",
            CategoryId = ConcreteAndAggregatesId,
            ItemClass = "MATERIAL",
            QuantityMode = "CONTINUOUS_BULK",
            BaseUnit = "m3",
            PackageType = null,
            AllowedUnits = ["m3", "cube", "ton"],
            AllowedPackageSizes = [],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "gravelSize", label = "Gravel Size", type = "select", required = true, options = new[] { "Fine (2-6mm)", "Medium (6-20mm)", "Coarse (20-60mm)", "Pea Gravel" } },
                new { id = "washed", label = "Washed / Clean", type = "select", required = false, options = new[] { "Washed", "Unwashed" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 11. Tiles (Finishes)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020B"),
            Name = "Tiles",
            CategoryId = FinishesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "sqm",
            PackageType = "BOX",
            AllowedUnits = ["sqm", "box"],
            AllowedPackageSizes = [1.44m, 1.0m, 1.08m, 1.92m, 2.16m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "material", label = "Material", type = "select", required = true, options = new[] { "Ceramic", "Porcelain", "Granite", "Marble", "Terracotta", "Glass Mosaic" } },
                new { id = "dimensionsMm", label = "Dimensions (mm)", type = "string", required = true, placeholder = "e.g. 600x600, 300x300, 300x600" },
                new { id = "piecesPerBox", label = "Pieces per Box", type = "number", required = false, placeholder = "e.g. 4" },
                new { id = "finish", label = "Finish", type = "select", required = false, options = new[] { "Polished / Gloss", "Matt", "Anti-Slip", "Textured / Rustic", "Honed" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 12. Tile Adhesive (Finishes)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020C"),
            Name = "Tile Adhesive",
            CategoryId = FinishesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "kg",
            PackageType = "BAG",
            AllowedUnits = ["kg", "bag"],
            AllowedPackageSizes = [20m, 25m, 40m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false, placeholder = "e.g. Weber, Laticrete, Bostik" },
                new { id = "adhesiveType", label = "Adhesive Type", type = "select", required = true, options = new[] { "Standard Cementitious (C1)", "Improved Flexible (C2TE)", "Epoxy Adhesive (R2)" } },
                new { id = "coverageArea", label = "Coverage Area (sqm/bag)", type = "string", required = false, placeholder = "e.g. 5-7 sqm" }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 13. Sealant (Finishes)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020D"),
            Name = "Sealant",
            CategoryId = FinishesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "cartridge",
            PackageType = "CARTRIDGE",
            AllowedUnits = ["cartridge", "piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "sealantType", label = "Sealant Type", type = "select", required = true, options = new[] { "Silicone (Acetic)", "Silicone (Neutral)", "Polyurethane (PU)", "Acrylic / Gap Filler" } },
                new { id = "colour", label = "Colour", type = "string", required = false, placeholder = "e.g. Clear, White, Grey, Black" },
                new { id = "volumeMl", label = "Cartridge Size (ml)", type = "select", required = false, options = new[] { "300ml", "310ml", "600ml sausage" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 14. Waterproofing (Finishes)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020E"),
            Name = "Waterproofing",
            CategoryId = FinishesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "L",
            PackageType = "CAN",
            AllowedUnits = ["L", "can", "kg", "bag"],
            AllowedPackageSizes = [4m, 10m, 20m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "waterproofingType", label = "Type", type = "select", required = true, options = new[] { "Liquid Applied Membrane", "Cementitious 2-Part System", "Bituminous Coating", "Polyurethane Coating" } },
                new { id = "applicationArea", label = "Recommended Area", type = "select", required = false, options = new[] { "Bathroom / Wet Areas", "Rooftop / Balcony", "Basement / Retaining Wall", "Water Tank" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 15. Timber (Timber & Boards)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000020F"),
            Name = "Timber",
            CategoryId = TimberAndBoardsId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "board", "m"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "woodSpecies", label = "Species / Type", type = "select", required = true, options = new[] { "Teak", "Mahogany", "Pine", "Kempas", "Treated Rubberwood", "Grandis", "Hardwood Mixed" } },
                new { id = "thicknessMm", label = "Thickness (mm)", type = "number", required = true, placeholder = "e.g. 25, 50" },
                new { id = "widthMm", label = "Width (mm)", type = "number", required = true, placeholder = "e.g. 50, 100, 150" },
                new { id = "lengthM", label = "Length (m)", type = "number", required = true, placeholder = "e.g. 2.4, 3.0, 3.6" },
                new { id = "treatment", label = "Treatment", type = "select", required = false, options = new[] { "Kiln Dried & Treated", "Air Dried", "Rough Sawn / Green" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 16. Plywood (Timber & Boards)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000210"),
            Name = "Plywood",
            CategoryId = TimberAndBoardsId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "sheet",
            PackageType = "SHEET",
            AllowedUnits = ["sheet", "piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "thicknessMm", label = "Thickness (mm)", type = "select", required = true, options = new[] { "3mm", "6mm", "9mm", "12mm", "15mm", "18mm", "25mm" } },
                new { id = "grade", label = "Grade / Spec", type = "select", required = true, options = new[] { "Commercial / Interior Plywood", "Marine Grade (Waterproof)", "Film Faced / Shuttering Plywood", "BWP (Boiling Water Proof)" } },
                new { id = "dimensions", label = "Sheet Dimensions", type = "select", required = false, options = new[] { "8ft x 4ft (2440 x 1220 mm)", "7ft x 3ft", "6ft x 3ft" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 17. Roofing Sheets (Roofing)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000211"),
            Name = "Roofing Sheets",
            CategoryId = RoofingId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "sheet",
            PackageType = "SHEET",
            AllowedUnits = ["sheet", "piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "material", label = "Material Type", type = "select", required = true, options = new[] { "Zinc-Alum / Colorbond Steel", "Polycarbonate (Clear/Tinted)", "Asbestos-Free Fibre Cement", "UPVC Multi-Wall", "Transparent Acrylic" } },
                new { id = "lengthM", label = "Length (m)", type = "number", required = true, placeholder = "e.g. 2.4, 3.0, 3.6, 6.0" },
                new { id = "widthM", label = "Width (m)", type = "number", required = false, placeholder = "e.g. 1.0" },
                new { id = "thicknessMm", label = "Thickness (mm)", type = "number", required = false, placeholder = "e.g. 0.47, 0.50, 1.2" },
                new { id = "profile", label = "Profile", type = "select", required = false, options = new[] { "Corrugated / Wave", "Trapezoidal / Box Rib", "Tile Look", "Standing Seam" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 18. PVC Pipes (Plumbing)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000212"),
            Name = "PVC Pipes",
            CategoryId = PlumbingId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIPE",
            AllowedUnits = ["piece", "pipe", "length"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "diameterMm", label = "Diameter (mm)", type = "select", required = true, options = new[] { "20mm (1/2\")", "25mm (3/4\")", "32mm (1\")", "40mm (1-1/4\")", "50mm (1-1/2\")", "63mm (2\")", "75mm (2-1/2\")", "90mm (3\")", "110mm (4\")", "160mm (6\")" } },
                new { id = "lengthPerPieceM", label = "Length per piece (m)", type = "number", required = true, placeholder = "e.g. 4 or 6" },
                new { id = "pressureClass", label = "Pressure Class", type = "select", required = false, options = new[] { "Type 400 (Low Pressure)", "Type 600 (Standard)", "Type 1000 (High Pressure)", "Non-Pressure / Drainage" } },
                new { id = "pipeType", label = "Pipe Type", type = "select", required = true, options = new[] { "Potable Cold Water", "Drainage / Waste / Vent (DWV)", "Electrical Conduit", "Rainwater Downpipe" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 19. Cable (Electrical)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000213"),
            Name = "Cable",
            CategoryId = ElectricalId,
            ItemClass = "MATERIAL",
            QuantityMode = "PACKAGE",
            BaseUnit = "m",
            PackageType = "ROLL",
            AllowedUnits = ["m", "roll"],
            AllowedPackageSizes = [50m, 100m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "conductorSizeMm2", label = "Conductor Size", type = "select", required = true, options = new[] { "1.0 mm\u00B2", "1.5 mm\u00B2", "2.5 mm\u00B2", "4.0 mm\u00B2", "6.0 mm\u00B2", "10.0 mm\u00B2", "16.0 mm\u00B2" } },
                new { id = "coreCount", label = "Cores", type = "select", required = true, options = new[] { "Single Core (1C)", "Twin & Earth (2C+E)", "3 Core", "4 Core Armoured" } },
                new { id = "insulationType", label = "Insulation", type = "select", required = false, options = new[] { "PVC", "XLPE", "LSZH (Low Smoke Zero Halogen)" } },
                new { id = "voltageRating", label = "Voltage Rating", type = "select", required = false, options = new[] { "300/500V", "450/750V", "600/1000V" } }
            }),
            PriceBasis = "PER_PACKAGE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 20. Doors (Doors / Windows / Fixtures)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000214"),
            Name = "Doors",
            CategoryId = DoorsWindowsFixturesId,
            ItemClass = "FIXTURE",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "doorType", label = "Door Type", type = "select", required = true, options = new[] { "Solid Wood Panel Door", "Flush Door (Hollow Core)", "Flush Door (Solid Core)", "Aluminium Framed Glass Door", "UPVC Bathroom Door", "Steel Security Door" } },
                new { id = "heightMm", label = "Height (mm)", type = "number", required = false, placeholder = "e.g. 2100" },
                new { id = "widthMm", label = "Width (mm)", type = "number", required = false, placeholder = "e.g. 900, 800, 750" },
                new { id = "finish", label = "Finish", type = "select", required = false, options = new[] { "Raw / Unfinished", "Stained & Varnished", "Primed", "Painted" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 21. Windows (Doors / Windows / Fixtures)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000215"),
            Name = "Windows",
            CategoryId = DoorsWindowsFixturesId,
            ItemClass = "FIXTURE",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "frameMaterial", label = "Frame Material", type = "select", required = true, options = new[] { "Aluminium (Powder Coated)", "Aluminium (Anodized)", "UPVC", "Timber", "Steel" } },
                new { id = "openingMechanism", label = "Opening Type", type = "select", required = true, options = new[] { "Sliding (2 Panel)", "Sliding (3/4 Panel)", "Casement", "Awning / Top Hung", "Fixed / Picture Window" } },
                new { id = "heightMm", label = "Height (mm)", type = "number", required = false, placeholder = "e.g. 1200" },
                new { id = "widthMm", label = "Width (mm)", type = "number", required = false, placeholder = "e.g. 1500" }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 22. Glass (Doors / Windows / Fixtures)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000216"),
            Name = "Glass",
            CategoryId = DoorsWindowsFixturesId,
            ItemClass = "MATERIAL",
            QuantityMode = "PIECE",
            BaseUnit = "sheet",
            PackageType = "SHEET",
            AllowedUnits = ["sheet", "piece", "sqm"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "glassType", label = "Glass Type", type = "select", required = true, options = new[] { "Clear Float", "Toughened / Tempered", "Laminated Safety Glass", "Frosted / Obscure", "Tinted / Solar Reflective" } },
                new { id = "thicknessMm", label = "Thickness (mm)", type = "select", required = true, options = new[] { "4mm", "5mm", "6mm", "8mm", "10mm", "12mm" } },
                new { id = "dimensions", label = "Sheet Dimensions (mm)", type = "string", required = false, placeholder = "e.g. 1830 x 1220" }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 23. Scaffolding (Temporary Works)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000217"),
            Name = "Scaffolding",
            CategoryId = TemporaryWorksId,
            ItemClass = "TEMPORARY_WORK",
            QuantityMode = "PIECE",
            BaseUnit = "set",
            PackageType = "PIECE",
            AllowedUnits = ["set", "piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "scaffoldType", label = "Scaffold Type", type = "select", required = true, options = new[] { "H-Frame Scaffolding Set", "Cuplock System", "Tube & Coupler", "Mobile Tower Scaffold (Castor Wheels)" } },
                new { id = "material", label = "Material", type = "select", required = false, options = new[] { "Galvanized Steel", "Painted Steel", "Aluminium" } },
                new { id = "componentsIncluded", label = "Components Included", type = "string", required = false, placeholder = "e.g. 2 Frames, 2 Cross Braces, 4 Joint Pins" }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 24. Formwork (Temporary Works)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000218"),
            Name = "Formwork",
            CategoryId = TemporaryWorksId,
            ItemClass = "TEMPORARY_WORK",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "SHEET",
            AllowedUnits = ["piece", "sheet", "sqm"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "formworkType", label = "Formwork Type", type = "select", required = true, options = new[] { "Steel Formwork Panels", "Aluminium Formwork Panels", "Film-Faced Shuttering Board", "Plastic Modular Formwork" } },
                new { id = "dimensions", label = "Dimensions", type = "string", required = false, placeholder = "e.g. 1200 x 600 mm" }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 25. Props (Temporary Works)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000219"),
            Name = "Props",
            CategoryId = TemporaryWorksId,
            ItemClass = "TEMPORARY_WORK",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "propType", label = "Prop Type", type = "select", required = true, options = new[] { "Acro Prop / Telescopic Steel Prop", "Heavy Duty Shoring Prop", "Push-Pull Prop" } },
                new { id = "extendedHeightM", label = "Max Extended Height", type = "select", required = true, options = new[] { "No. 0 (1.0m - 1.8m)", "No. 1 (1.75m - 3.1m)", "No. 2 (2.0m - 3.4m)", "No. 3 (2.6m - 4.0m)", "No. 4 (3.2m - 4.9m)" } },
                new { id = "safeWorkingLoadKg", label = "Safe Working Load (kg)", type = "number", required = false, placeholder = "e.g. 1500" }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 26. Drill (Construction Tools)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021A"),
            Name = "Drill",
            CategoryId = ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = true, placeholder = "e.g. Bosch, Makita, DeWalt, DongCheng" },
                new { id = "model", label = "Model", type = "string", required = false, placeholder = "e.g. GBH 2-26 DRE" },
                new { id = "drillType", label = "Drill Type", type = "select", required = true, options = new[] { "Rotary Hammer Drill (SDS-Plus)", "Heavy Demolition Hammer (SDS-Max)", "Impact Drill", "Cordless Combi Drill" } },
                new { id = "powerSource", label = "Power Source", type = "select", required = true, options = new[] { "Corded 230V Electric", "Cordless 18V/20V Battery", "Cordless 36V/40V Max" } },
                new { id = "wattage", label = "Wattage (W)", type = "number", required = false, placeholder = "e.g. 800" }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 27. Angle Grinder (Construction Tools)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021B"),
            Name = "Angle Grinder",
            CategoryId = ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = true, placeholder = "e.g. Bosch, Makita, DeWalt" },
                new { id = "model", label = "Model", type = "string", required = false },
                new { id = "discDiameterMm", label = "Disc Diameter", type = "select", required = true, options = new[] { "100mm (4 inch)", "115mm (4.5 inch)", "125mm (5 inch)", "180mm (7 inch)", "230mm (9 inch)" } },
                new { id = "wattage", label = "Wattage (W)", type = "number", required = false, placeholder = "e.g. 850, 2200" },
                new { id = "powerSource", label = "Power Source", type = "select", required = true, options = new[] { "Corded 230V", "Cordless 18V/20V Battery" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 28. Circular Saw (Construction Tools)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021C"),
            Name = "Circular Saw",
            CategoryId = ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = true, placeholder = "e.g. Makita, Bosch, DeWalt" },
                new { id = "model", label = "Model", type = "string", required = false },
                new { id = "bladeDiameterMm", label = "Blade Diameter", type = "select", required = true, options = new[] { "165mm (6.5\")", "185mm (7.25\")", "235mm (9.25\")" } },
                new { id = "powerSource", label = "Power Source", type = "select", required = true, options = new[] { "Corded 230V", "Cordless 18V/40V Battery" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 29. Welding Machine (Construction Tools)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021D"),
            Name = "Welding Machine",
            CategoryId = ConstructionToolsId,
            ItemClass = "TOOL",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = true, placeholder = "e.g. Jasic, Riland, Miller, Lincoln" },
                new { id = "weldingType", label = "Welding Process", type = "select", required = true, options = new[] { "MMA / Inverter Arc Welder", "MIG / MAG (Gas & Gasless)", "TIG Welder", "Multi-Process MIG/TIG/MMA" } },
                new { id = "currentAmps", label = "Max Current (Amps)", type = "number", required = true, placeholder = "e.g. 160, 200, 250, 400" },
                new { id = "inputVoltage", label = "Input Voltage", type = "select", required = false, options = new[] { "Single Phase 230V", "Three Phase 400V", "Dual Voltage 230V/400V" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 30. Air Compressor (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021E"),
            Name = "Air Compressor",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = true, placeholder = "e.g. Puma, Atlas Copco, Ingersoll Rand" },
                new { id = "model", label = "Model", type = "string", required = false },
                new { id = "tankCapacityL", label = "Tank Capacity (L)", type = "number", required = true, placeholder = "e.g. 50, 100, 200, 300" },
                new { id = "maxPressureBar", label = "Max Pressure (Bar / PSI)", type = "string", required = false, placeholder = "e.g. 8 bar (116 psi) or 10 bar" },
                new { id = "motorPowerHp", label = "Motor Power (HP)", type = "number", required = true, placeholder = "e.g. 2, 3, 5.5, 7.5" },
                new { id = "powerSource", label = "Power Source", type = "select", required = true, options = new[] { "Electric 230V (Single Phase)", "Electric 400V (Three Phase)", "Petrol Engine", "Diesel Engine" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 31. Concrete Mixer (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-00000000021F"),
            Name = "Concrete Mixer",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false, placeholder = "e.g. Winget, Belle, Local Heavy Duty" },
                new { id = "drumCapacityL", label = "Drum Capacity (Litres or Bags)", type = "select", required = true, options = new[] { "Half Bag (140L)", "1 Bag (200L - 250L)", "2 Bag (400L - 500L)" } },
                new { id = "powerSource", label = "Engine / Power Source", type = "select", required = true, options = new[] { "Diesel Engine (e.g. Yanmar/Kubota/Kirloskar)", "Petrol Engine (e.g. Honda)", "Electric Motor 230V", "Electric Motor 400V" } },
                new { id = "isTowable", label = "Towable / Mobile", type = "select", required = false, options = new[] { "Yes (Pneumatic Wheels & Towbar)", "Stationary / Site Wheels" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 32. Water Pump (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000220"),
            Name = "Water Pump",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false, placeholder = "e.g. Honda, Koshin, Tsurumi" },
                new { id = "pumpType", label = "Pump Type", type = "select", required = true, options = new[] { "Self-Priming Centrifugal / Dewatering", "Submersible Sludge / Trash Pump", "High Pressure Fire / Irrigation Pump" } },
                new { id = "inletOutletSizeMm", label = "Inlet/Outlet Size", type = "select", required = true, options = new[] { "2 inch (50mm)", "3 inch (75mm)", "4 inch (100mm)" } },
                new { id = "powerSource", label = "Power Source", type = "select", required = true, options = new[] { "Petrol Engine", "Diesel Engine", "Electric 230V", "Electric 400V" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 33. Compactor (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000221"),
            Name = "Compactor",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false, placeholder = "e.g. Wacker Neuson, Mikasa, Bomag" },
                new { id = "compactorType", label = "Compactor Type", type = "select", required = true, options = new[] { "Forward Plate Compactor", "Reversible Plate Compactor", "Tamping Rammer (Jumping Jack)", "Walk-Behind Vibratory Roller" } },
                new { id = "operatingWeightKg", label = "Operating Weight (kg)", type = "number", required = false, placeholder = "e.g. 70, 90, 160" },
                new { id = "engineType", label = "Engine Type", type = "select", required = true, options = new[] { "Petrol Engine (Honda GX)", "Diesel Engine (Hatz/Yanmar)" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 34. Concrete Vibrator (Machinery / Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000222"),
            Name = "Concrete Vibrator",
            CategoryId = MachineryAndEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "unit"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false },
                new { id = "needleDiameterMm", label = "Needle Diameter (mm)", type = "select", required = true, options = new[] { "25mm", "35mm", "45mm", "50mm", "60mm" } },
                new { id = "shaftLengthM", label = "Flexible Shaft Length (m)", type = "select", required = true, options = new[] { "4m", "5m", "6m" } },
                new { id = "driveUnit", label = "Drive Unit", type = "select", required = true, options = new[] { "Petrol Engine Drive", "Electric Motor Drive (230V)", "Handheld Portable Electric" } }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 35. Fall Protection Harness (Site/Safety Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000223"),
            Name = "Fall Protection Harness",
            CategoryId = SiteSafetyEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece", "set"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "brand", label = "Brand", type = "string", required = false, placeholder = "e.g. 3M DBI-SALA, Karam, Miller" },
                new { id = "certificationStandard", label = "Safety Certification", type = "select", required = true, options = new[] { "EN 361 (CE Certified)", "ANSI Z359", "OSHA Compliant" } },
                new { id = "attachmentPoints", label = "Attachment D-Rings", type = "select", required = false, options = new[] { "1-Point (Dorsal)", "2-Point (Dorsal & Sternal)", "4-Point (Dorsal, Sternal, Work Positioning)" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 36. Safety Helmet / Hard Hat (Site/Safety Equipment)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000224"),
            Name = "Safety Helmet",
            CategoryId = SiteSafetyEquipmentId,
            ItemClass = "EQUIPMENT",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = "PIECE",
            AllowedUnits = ["piece"],
            AllowedPackageSizes = [1m],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "standard", label = "Safety Standard", type = "select", required = true, options = new[] { "EN 397 (Industrial)", "ANSI/ISEA Z89.1 Type 1 Class E", "SLS 614" } },
                new { id = "colour", label = "Helmet Colour", type = "select", required = true, options = new[] { "White (Engineers/Managers)", "Yellow (General Labor)", "Blue (Electricians/Carpenters)", "Green (Safety Officers)", "Orange", "Red" } },
                new { id = "suspensionType", label = "Suspension", type = "select", required = false, options = new[] { "Ratchet Wheel Adjustment", "Pin-Lock Adjustment" } }
            }),
            PriceBasis = "PER_PIECE",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        },

        // 37. Custom / Legacy Construction Item (Fallback)
        new()
        {
            Id = new("00000000-0000-0000-0000-000000000099"),
            Name = "Custom Construction Item",
            CategoryId = MiscSurplusId,
            ItemClass = "OTHER_CONSTRUCTION",
            QuantityMode = "PIECE",
            BaseUnit = "piece",
            PackageType = null,
            AllowedUnits = ["piece", "kg", "m", "sqm", "m3", "bag", "box", "can", "unit"],
            AllowedPackageSizes = [],
            AttributeSchema = JsonSerializer.Serialize(new object[]
            {
                new { id = "itemName", label = "Item Name", type = "string", required = true, placeholder = "What construction item are you listing?" },
                new { id = "soldAs", label = "Sold As", type = "select", required = true, options = new[] { "Individual piece / unit", "Package / container", "Continuous bulk / volume / weight" } },
                new { id = "specifications", label = "Specifications & Dimensions", type = "string", required = false, placeholder = "e.g. Dimensions, material, grade, capacity" }
            }),
            PriceBasis = "PER_UNIT",
            IsActive = true,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp
        }
    ]);

    // The schema is the single source of truth for mobile/web forms and a future
    // requirement assistant.  UI code must not carry a second list of local presets.
    private static ConstructionItemTemplate[] AddSriLankanFormMetadata(ConstructionItemTemplate[] templates)
    {
        foreach (var template in templates)
        {
            if (JsonNode.Parse(template.AttributeSchema) is not JsonArray fields) continue;
            foreach (var node in fields.OfType<JsonObject>())
            {
                var id = node["id"]?.GetValue<string>() ?? string.Empty;
                var label = node["label"]?.GetValue<string>() ?? id;
                // Legacy seeds were written as engineering data sheets. Inventory can
                // be identified safely by item, condition, quantity, price and location;
                // unknown technical details remain null instead of receiving fake defaults.
                // Paint type and colour are the small exception: together they are
                // the ordinary identifying detail needed to price a paint listing.
                var requiredForSafeIdentification = template.Name == "Paint" && id is "paintType" or "colour";
                node["required"] = requiredForSafeIdentification;
                node["priority"] = requiredForSafeIdentification ? "REQUIRED" : IsRecommended(id) ? "RECOMMENDED" : "OPTIONAL";
                node["sellerField"] = true;
                node["buyerPreference"] = IsBuyerPreference(template.Name, id);
                node["allowOther"] = node["type"]?.GetValue<string>() == "select";
                node["labelI18n"] = Localized(label);
                node["helper"] = Helper(template.Name, id);
                if (node["type"]?.GetValue<string>() == "select" && node["options"] is JsonArray options &&
                    !options.Any(value => string.Equals(value?.GetValue<string>(), "Other", StringComparison.OrdinalIgnoreCase)))
                {
                    options.Add("Other");
                }
            }
            template.AttributeSchema = fields.ToJsonString();
        }
        return templates;
    }

    private static bool IsRecommended(string id) => id is "brickType" or "cementType" or "paintType" or "material" or "diameterMm" or "capacityKva" or "fuelType" or "drillType";

    private static bool IsBuyerPreference(string item, string id) => (item, id) switch
    {
        ("Bricks", "brickType") or ("Bricks", "dimensionsMm") or
        ("Paint", "paintType") or ("Paint", "finish") or
        ("Tiles", "material") or ("Tiles", "dimensionsMm") or ("Tiles", "finish") or
        ("Generator", "capacityKva") or ("Generator", "fuelType") or
        ("Drill", "drillType") => true,
        _ => false
    };

    private static string Helper(string item, string id) => (item, id) switch
    {
        ("Bricks", "dimensionsMm") => "Choose the closest size. Select Other for a different size.",
        ("Bricks", "compressiveStrength") => "Additional specification (optional).",
        ("Generator", "capacityKva") => "Usually written on the generator label.",
        ("Reinforcement Steel", "diameterMm") => "Choose the diameter printed on the bar tag.",
        ("Tiles", "piecesPerBox") => "Number of individual tiles inside one box.",
        _ => string.Empty
    };

    private static JsonObject Localized(string english)
    {
        var translations = english switch
        {
            "Brick Type" => ("ගඩොල් වර්ගය", "செங்கல் வகை"),
            "Dimensions (mm)" => ("ප්‍රමාණය (මි.මී.)", "அளவு (மிமீ)"),
            "Cement Type" => ("සිමෙන්ති වර්ගය", "சிமெந்து வகை"),
            "Paint Type" => ("තීන්ත වර්ගය", "பெயிண்ட் வகை"),
            "Finish" => ("නිමාව", "பூச்சு"),
            "Capacity (kVA)" => ("ධාරිතාව (kVA)", "திறன் (kVA)"),
            "Fuel Type" => ("ඉන්ධන වර්ගය", "எரிபொருள் வகை"),
            "Diameter (mm)" => ("විෂ්කම්භය (මි.මී.)", "விட்டம் (மிமீ)"),
            _ => (english, english)
        };
        return new JsonObject { ["en"] = english, ["si"] = translations.Item1, ["ta"] = translations.Item2 };
    }
}
