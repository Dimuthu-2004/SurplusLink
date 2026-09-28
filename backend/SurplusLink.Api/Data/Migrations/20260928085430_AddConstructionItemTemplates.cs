using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionItemTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConstructionItemTemplateId",
                table: "Listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomPendingReview",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationsJson",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConstructionItemTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuantityMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BaseUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PackageType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AllowedUnits = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    AllowedPackageSizes = table.Column<decimal[]>(type: "numeric[]", nullable: false, defaultValueSql: "ARRAY[]::numeric[]"),
                    AttributeSchema = table.Column<string>(type: "text", nullable: false),
                    PriceBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "PER_UNIT"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionItemTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructionItemTemplates_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                column: "AllowedUnits",
                value: new[] { "kg", "bag", "ton", "piece" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                column: "AllowedUnits",
                value: new[] { "piece", "rod", "kg", "ton", "m" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                column: "AllowedUnits",
                value: new[] { "piece", "board", "sqm", "m" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                column: "AllowedUnits",
                value: new[] { "piece", "block", "sqm" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "AllowedUnits",
                value: new[] { "m3", "ton", "cube", "kg" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "AllowedUnits",
                value: new[] { "sqm", "box", "piece" });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "AllowedUnits", "CreatedAtUtc", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000110"), new[] { "piece", "block", "rod", "kg", "ton", "m" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Structural & Masonry", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000111"), new[] { "kg", "bag", "ton", "m3", "cube" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Concrete & Aggregates", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000112"), new[] { "L", "can", "sqm", "box", "kg", "bag", "cartridge", "piece" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Finishes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000113"), new[] { "piece", "sheet", "m", "sqm" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Timber & Boards", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000114"), new[] { "sheet", "piece", "sqm", "m" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Roofing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000115"), new[] { "piece", "pipe", "length", "pack", "m" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plumbing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000116"), new[] { "piece", "roll", "pack", "m" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Electrical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000117"), new[] { "piece", "set", "sheet" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doors / Windows / Fixtures", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000118"), new[] { "piece", "set", "sheet", "unit" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Temporary Works", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000119"), new[] { "piece", "unit", "set" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Construction Tools", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000011a"), new[] { "piece", "unit" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Machinery / Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000011b"), new[] { "piece", "set", "pack" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Site/Safety Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000011c"), new[] { "piece", "unit", "kg", "m", "sqm", "m3", "bag", "box", "can" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Miscellaneous Construction Surplus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ConstructionItemTemplates",
                columns: new[] { "Id", "AllowedPackageSizes", "AllowedUnits", "AttributeSchema", "BaseUnit", "CategoryId", "CreatedAtUtc", "IsActive", "ItemClass", "Name", "PackageType", "PriceBasis", "QuantityMode", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000099"), new decimal[0], new[] { "piece", "kg", "m", "sqm", "m3", "bag", "box", "can", "unit" }, "[{\"id\":\"itemName\",\"label\":\"Item Name\",\"type\":\"string\",\"required\":true,\"placeholder\":\"What construction item are you listing?\"},{\"id\":\"soldAs\",\"label\":\"Sold As\",\"type\":\"select\",\"required\":true,\"options\":[\"Individual piece / unit\",\"Package / container\",\"Continuous bulk / volume / weight\"]},{\"id\":\"specifications\",\"label\":\"Specifications \\u0026 Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dimensions, material, grade, capacity\"}]", "piece", new Guid("00000000-0000-0000-0000-00000000011c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OTHER_CONSTRUCTION", "Custom Construction Item", null, "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000201"), new[] { 1m, 4m, 10m, 20m }, new[] { "L", "can" }, "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dulux, Asian Paints, Nippon\"},{\"id\":\"paintType\",\"label\":\"Paint Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Emulsion\",\"Gloss / Enamel\",\"Weather-shield / Exterior\",\"Primer\",\"Undercoat\",\"Epoxy\"]},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Brilliant White, Cream, Slate Grey\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Matt\",\"Satin\",\"Semi-Gloss\",\"High Gloss\",\"Eggshell\"]}]", "L", new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Paint", "CAN", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000202"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Perkins, Cummins, Honda, Denyo\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. DG6500, EU22i\"},{\"id\":\"capacityKva\",\"label\":\"Capacity (kVA)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 5, 10, 50, 100\"},{\"id\":\"fuelType\",\"label\":\"Fuel Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Diesel\",\"Petrol\",\"Gas\",\"Dual Fuel\"]},{\"id\":\"phase\",\"label\":\"Phase\",\"type\":\"select\",\"required\":true,\"options\":[\"Single Phase\",\"Three Phase\"]},{\"id\":\"voltage\",\"label\":\"Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"230V\",\"400V\",\"110V/230V Dual\"]},{\"id\":\"runningHours\",\"label\":\"Running Hours\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 450\"}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Generator", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000203"), new[] { 50m, 25m }, new[] { "kg", "bag" }, "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Tokyo Super, Insee Sanstha, Ultratech\"},{\"id\":\"cementType\",\"label\":\"Cement Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Ordinary Portland Cement (OPC)\",\"Portland Pozzolana Cement (PPC)\",\"Rapid Hardening\",\"White Cement\",\"Masonry Cement\"]},{\"id\":\"grade\",\"label\":\"Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"Grade 43\",\"Grade 53\",\"Blended / Standard\"]}]", "kg", new Guid("00000000-0000-0000-0000-000000000111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Cement", "BAG", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000204"), new[] { 1m }, new[] { "piece", "rod", "kg", "ton" }, "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"6mm\",\"8mm\",\"10mm\",\"12mm\",\"16mm\",\"20mm\",\"25mm\",\"32mm\"]},{\"id\":\"lengthM\",\"label\":\"Length per rod (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 6 or 12\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":true,\"options\":[\"RB500 / Grade 500\",\"RB415 / Grade 415\",\"Mild Steel Grade 250\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000110"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Reinforcement Steel", "ROD", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000205"), new[] { 1m }, new[] { "piece", "length", "ton" }, "[{\"id\":\"sectionType\",\"label\":\"Section Type\",\"type\":\"select\",\"required\":true,\"options\":[\"I-Beam / Universal Beam\",\"H-Column\",\"C-Channel\",\"Equal Angle (L-section)\",\"Box Section / Hollow Tube (SHS/RHS)\",\"Flat Bar\"]},{\"id\":\"dimensions\",\"label\":\"Dimensions / Spec\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. 150x75mm, 100x50x4mm\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 6.0\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"S275\",\"S355\",\"Grade 43\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000110"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Structural Steel", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000206"), new[] { 1m }, new[] { "piece" }, "[{\"id\":\"brickType\",\"label\":\"Brick Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Red Clay Wire-Cut\",\"Traditional Burnt Clay\",\"Fly Ash\",\"Engineering Brick\",\"Refractory / Fire Brick\"]},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 215 x 102.5 x 65\"},{\"id\":\"compressiveStrength\",\"label\":\"Compressive Strength (N/mm\\u00B2)\",\"type\":\"string\",\"required\":false}]", "piece", new Guid("00000000-0000-0000-0000-000000000110"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Bricks", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000207"), new[] { 1m }, new[] { "piece", "block" }, "[{\"id\":\"blockType\",\"label\":\"Block Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Cement Solid Block\",\"Cement Hollow Block\",\"Autoclaved Aerated Concrete (AAC)\",\"Cellular Lightweight Concrete (CLC)\"]},{\"id\":\"thicknessInches\",\"label\":\"Thickness\",\"type\":\"select\",\"required\":true,\"options\":[\"4 inch\",\"6 inch\",\"8 inch\",\"9 inch\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000110"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Blocks", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000208"), new decimal[0], new[] { "m3", "cube", "ton" }, "[{\"id\":\"sandType\",\"label\":\"Sand Type\",\"type\":\"select\",\"required\":true,\"options\":[\"River Sand\",\"Manufactured Sand (M-Sand)\",\"Plastering Sand (P-Sand)\",\"Filling Sand\"]},{\"id\":\"screeningStatus\",\"label\":\"Screening\",\"type\":\"select\",\"required\":false,\"options\":[\"Screened / Washed\",\"Unscreened\"]}]", "m3", new Guid("00000000-0000-0000-0000-000000000111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Sand", null, "PER_UNIT", "CONTINUOUS_BULK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000209"), new decimal[0], new[] { "m3", "cube", "ton" }, "[{\"id\":\"aggregateSize\",\"label\":\"Aggregate Size\",\"type\":\"select\",\"required\":true,\"options\":[\"10mm (3/8\\u0022)\",\"20mm (3/4\\u0022)\",\"40mm (1.5\\u0022)\",\"ABC (Aggregate Base Coarse)\",\"Quarry Dust\"]},{\"id\":\"aggregateType\",\"label\":\"Source Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Crushed Granite Rock\",\"Limestone\",\"Gravel Aggregate\"]}]", "m3", new Guid("00000000-0000-0000-0000-000000000111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Aggregate", null, "PER_UNIT", "CONTINUOUS_BULK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020a"), new decimal[0], new[] { "m3", "cube", "ton" }, "[{\"id\":\"gravelSize\",\"label\":\"Gravel Size\",\"type\":\"select\",\"required\":true,\"options\":[\"Fine (2-6mm)\",\"Medium (6-20mm)\",\"Coarse (20-60mm)\",\"Pea Gravel\"]},{\"id\":\"washed\",\"label\":\"Washed / Clean\",\"type\":\"select\",\"required\":false,\"options\":[\"Washed\",\"Unwashed\"]}]", "m3", new Guid("00000000-0000-0000-0000-000000000111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Gravel", null, "PER_UNIT", "CONTINUOUS_BULK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020b"), new[] { 1.44m, 1.0m, 1.08m, 1.92m, 2.16m }, new[] { "sqm", "box" }, "[{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":true,\"options\":[\"Ceramic\",\"Porcelain\",\"Granite\",\"Marble\",\"Terracotta\",\"Glass Mosaic\"]},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. 600x600, 300x300, 300x600\"},{\"id\":\"piecesPerBox\",\"label\":\"Pieces per Box\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 4\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Polished / Gloss\",\"Matt\",\"Anti-Slip\",\"Textured / Rustic\",\"Honed\"]}]", "sqm", new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Tiles", "BOX", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020c"), new[] { 20m, 25m, 40m }, new[] { "kg", "bag" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Weber, Laticrete, Bostik\"},{\"id\":\"adhesiveType\",\"label\":\"Adhesive Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Standard Cementitious (C1)\",\"Improved Flexible (C2TE)\",\"Epoxy Adhesive (R2)\"]},{\"id\":\"coverageArea\",\"label\":\"Coverage Area (sqm/bag)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 5-7 sqm\"}]", "kg", new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Tile Adhesive", "BAG", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020d"), new[] { 1m }, new[] { "cartridge", "piece" }, "[{\"id\":\"sealantType\",\"label\":\"Sealant Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Silicone (Acetic)\",\"Silicone (Neutral)\",\"Polyurethane (PU)\",\"Acrylic / Gap Filler\"]},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Clear, White, Grey, Black\"},{\"id\":\"volumeMl\",\"label\":\"Cartridge Size (ml)\",\"type\":\"select\",\"required\":false,\"options\":[\"300ml\",\"310ml\",\"600ml sausage\"]}]", "cartridge", new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Sealant", "CARTRIDGE", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020e"), new[] { 4m, 10m, 20m }, new[] { "L", "can", "kg", "bag" }, "[{\"id\":\"waterproofingType\",\"label\":\"Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Liquid Applied Membrane\",\"Cementitious 2-Part System\",\"Bituminous Coating\",\"Polyurethane Coating\"]},{\"id\":\"applicationArea\",\"label\":\"Recommended Area\",\"type\":\"select\",\"required\":false,\"options\":[\"Bathroom / Wet Areas\",\"Rooftop / Balcony\",\"Basement / Retaining Wall\",\"Water Tank\"]}]", "L", new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Waterproofing", "CAN", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000020f"), new[] { 1m }, new[] { "piece", "board", "m" }, "[{\"id\":\"woodSpecies\",\"label\":\"Species / Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Teak\",\"Mahogany\",\"Pine\",\"Kempas\",\"Treated Rubberwood\",\"Grandis\",\"Hardwood Mixed\"]},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 25, 50\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 50, 100, 150\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2.4, 3.0, 3.6\"},{\"id\":\"treatment\",\"label\":\"Treatment\",\"type\":\"select\",\"required\":false,\"options\":[\"Kiln Dried \\u0026 Treated\",\"Air Dried\",\"Rough Sawn / Green\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000113"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Timber", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000210"), new[] { 1m }, new[] { "sheet", "piece" }, "[{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"3mm\",\"6mm\",\"9mm\",\"12mm\",\"15mm\",\"18mm\",\"25mm\"]},{\"id\":\"grade\",\"label\":\"Grade / Spec\",\"type\":\"select\",\"required\":true,\"options\":[\"Commercial / Interior Plywood\",\"Marine Grade (Waterproof)\",\"Film Faced / Shuttering Plywood\",\"BWP (Boiling Water Proof)\"]},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions\",\"type\":\"select\",\"required\":false,\"options\":[\"8ft x 4ft (2440 x 1220 mm)\",\"7ft x 3ft\",\"6ft x 3ft\"]}]", "sheet", new Guid("00000000-0000-0000-0000-000000000113"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Plywood", "SHEET", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000211"), new[] { 1m }, new[] { "sheet", "piece" }, "[{\"id\":\"material\",\"label\":\"Material Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Zinc-Alum / Colorbond Steel\",\"Polycarbonate (Clear/Tinted)\",\"Asbestos-Free Fibre Cement\",\"UPVC Multi-Wall\",\"Transparent Acrylic\"]},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2.4, 3.0, 3.6, 6.0\"},{\"id\":\"widthM\",\"label\":\"Width (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1.0\"},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 0.47, 0.50, 1.2\"},{\"id\":\"profile\",\"label\":\"Profile\",\"type\":\"select\",\"required\":false,\"options\":[\"Corrugated / Wave\",\"Trapezoidal / Box Rib\",\"Tile Look\",\"Standing Seam\"]}]", "sheet", new Guid("00000000-0000-0000-0000-000000000114"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Roofing Sheets", "SHEET", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000212"), new[] { 1m }, new[] { "piece", "pipe", "length" }, "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"20mm (1/2\\u0022)\",\"25mm (3/4\\u0022)\",\"32mm (1\\u0022)\",\"40mm (1-1/4\\u0022)\",\"50mm (1-1/2\\u0022)\",\"63mm (2\\u0022)\",\"75mm (2-1/2\\u0022)\",\"90mm (3\\u0022)\",\"110mm (4\\u0022)\",\"160mm (6\\u0022)\"]},{\"id\":\"lengthPerPieceM\",\"label\":\"Length per piece (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 4 or 6\"},{\"id\":\"pressureClass\",\"label\":\"Pressure Class\",\"type\":\"select\",\"required\":false,\"options\":[\"Type 400 (Low Pressure)\",\"Type 600 (Standard)\",\"Type 1000 (High Pressure)\",\"Non-Pressure / Drainage\"]},{\"id\":\"pipeType\",\"label\":\"Pipe Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Potable Cold Water\",\"Drainage / Waste / Vent (DWV)\",\"Electrical Conduit\",\"Rainwater Downpipe\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000115"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "PVC Pipes", "PIPE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000213"), new[] { 50m, 100m }, new[] { "m", "roll" }, "[{\"id\":\"conductorSizeMm2\",\"label\":\"Conductor Size\",\"type\":\"select\",\"required\":true,\"options\":[\"1.0 mm\\u00B2\",\"1.5 mm\\u00B2\",\"2.5 mm\\u00B2\",\"4.0 mm\\u00B2\",\"6.0 mm\\u00B2\",\"10.0 mm\\u00B2\",\"16.0 mm\\u00B2\"]},{\"id\":\"coreCount\",\"label\":\"Cores\",\"type\":\"select\",\"required\":true,\"options\":[\"Single Core (1C)\",\"Twin \\u0026 Earth (2C\\u002BE)\",\"3 Core\",\"4 Core Armoured\"]},{\"id\":\"insulationType\",\"label\":\"Insulation\",\"type\":\"select\",\"required\":false,\"options\":[\"PVC\",\"XLPE\",\"LSZH (Low Smoke Zero Halogen)\"]},{\"id\":\"voltageRating\",\"label\":\"Voltage Rating\",\"type\":\"select\",\"required\":false,\"options\":[\"300/500V\",\"450/750V\",\"600/1000V\"]}]", "m", new Guid("00000000-0000-0000-0000-000000000116"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Cable", "ROLL", "PER_PACKAGE", "PACKAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000214"), new[] { 1m }, new[] { "piece" }, "[{\"id\":\"doorType\",\"label\":\"Door Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Solid Wood Panel Door\",\"Flush Door (Hollow Core)\",\"Flush Door (Solid Core)\",\"Aluminium Framed Glass Door\",\"UPVC Bathroom Door\",\"Steel Security Door\"]},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2100\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 900, 800, 750\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Raw / Unfinished\",\"Stained \\u0026 Varnished\",\"Primed\",\"Painted\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000117"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "FIXTURE", "Doors", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000215"), new[] { 1m }, new[] { "piece" }, "[{\"id\":\"frameMaterial\",\"label\":\"Frame Material\",\"type\":\"select\",\"required\":true,\"options\":[\"Aluminium (Powder Coated)\",\"Aluminium (Anodized)\",\"UPVC\",\"Timber\",\"Steel\"]},{\"id\":\"openingMechanism\",\"label\":\"Opening Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Sliding (2 Panel)\",\"Sliding (3/4 Panel)\",\"Casement\",\"Awning / Top Hung\",\"Fixed / Picture Window\"]},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1200\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\"}]", "piece", new Guid("00000000-0000-0000-0000-000000000117"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "FIXTURE", "Windows", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000216"), new[] { 1m }, new[] { "sheet", "piece", "sqm" }, "[{\"id\":\"glassType\",\"label\":\"Glass Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Clear Float\",\"Toughened / Tempered\",\"Laminated Safety Glass\",\"Frosted / Obscure\",\"Tinted / Solar Reflective\"]},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"4mm\",\"5mm\",\"6mm\",\"8mm\",\"10mm\",\"12mm\"]},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1830 x 1220\"}]", "sheet", new Guid("00000000-0000-0000-0000-000000000117"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MATERIAL", "Glass", "SHEET", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000217"), new[] { 1m }, new[] { "set", "piece" }, "[{\"id\":\"scaffoldType\",\"label\":\"Scaffold Type\",\"type\":\"select\",\"required\":true,\"options\":[\"H-Frame Scaffolding Set\",\"Cuplock System\",\"Tube \\u0026 Coupler\",\"Mobile Tower Scaffold (Castor Wheels)\"]},{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":false,\"options\":[\"Galvanized Steel\",\"Painted Steel\",\"Aluminium\"]},{\"id\":\"componentsIncluded\",\"label\":\"Components Included\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 2 Frames, 2 Cross Braces, 4 Joint Pins\"}]", "set", new Guid("00000000-0000-0000-0000-000000000118"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TEMPORARY_WORK", "Scaffolding", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000218"), new[] { 1m }, new[] { "piece", "sheet", "sqm" }, "[{\"id\":\"formworkType\",\"label\":\"Formwork Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Steel Formwork Panels\",\"Aluminium Formwork Panels\",\"Film-Faced Shuttering Board\",\"Plastic Modular Formwork\"]},{\"id\":\"dimensions\",\"label\":\"Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1200 x 600 mm\"}]", "piece", new Guid("00000000-0000-0000-0000-000000000118"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TEMPORARY_WORK", "Formwork", "SHEET", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000219"), new[] { 1m }, new[] { "piece" }, "[{\"id\":\"propType\",\"label\":\"Prop Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Acro Prop / Telescopic Steel Prop\",\"Heavy Duty Shoring Prop\",\"Push-Pull Prop\"]},{\"id\":\"extendedHeightM\",\"label\":\"Max Extended Height\",\"type\":\"select\",\"required\":true,\"options\":[\"No. 0 (1.0m - 1.8m)\",\"No. 1 (1.75m - 3.1m)\",\"No. 2 (2.0m - 3.4m)\",\"No. 3 (2.6m - 4.0m)\",\"No. 4 (3.2m - 4.9m)\"]},{\"id\":\"safeWorkingLoadKg\",\"label\":\"Safe Working Load (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\"}]", "piece", new Guid("00000000-0000-0000-0000-000000000118"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TEMPORARY_WORK", "Props", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021a"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Bosch, Makita, DeWalt, DongCheng\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. GBH 2-26 DRE\"},{\"id\":\"drillType\",\"label\":\"Drill Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Rotary Hammer Drill (SDS-Plus)\",\"Heavy Demolition Hammer (SDS-Max)\",\"Impact Drill\",\"Cordless Combi Drill\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V Electric\",\"Cordless 18V/20V Battery\",\"Cordless 36V/40V Max\"]},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 800\"}]", "piece", new Guid("00000000-0000-0000-0000-000000000119"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TOOL", "Drill", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021b"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Bosch, Makita, DeWalt\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"discDiameterMm\",\"label\":\"Disc Diameter\",\"type\":\"select\",\"required\":true,\"options\":[\"100mm (4 inch)\",\"115mm (4.5 inch)\",\"125mm (5 inch)\",\"180mm (7 inch)\",\"230mm (9 inch)\"]},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 850, 2200\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V\",\"Cordless 18V/20V Battery\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000119"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TOOL", "Angle Grinder", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021c"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Makita, Bosch, DeWalt\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"bladeDiameterMm\",\"label\":\"Blade Diameter\",\"type\":\"select\",\"required\":true,\"options\":[\"165mm (6.5\\u0022)\",\"185mm (7.25\\u0022)\",\"235mm (9.25\\u0022)\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V\",\"Cordless 18V/40V Battery\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000119"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TOOL", "Circular Saw", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021d"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Jasic, Riland, Miller, Lincoln\"},{\"id\":\"weldingType\",\"label\":\"Welding Process\",\"type\":\"select\",\"required\":true,\"options\":[\"MMA / Inverter Arc Welder\",\"MIG / MAG (Gas \\u0026 Gasless)\",\"TIG Welder\",\"Multi-Process MIG/TIG/MMA\"]},{\"id\":\"currentAmps\",\"label\":\"Max Current (Amps)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 160, 200, 250, 400\"},{\"id\":\"inputVoltage\",\"label\":\"Input Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"Single Phase 230V\",\"Three Phase 400V\",\"Dual Voltage 230V/400V\"]}]", "piece", new Guid("00000000-0000-0000-0000-000000000119"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TOOL", "Welding Machine", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021e"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Puma, Atlas Copco, Ingersoll Rand\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"tankCapacityL\",\"label\":\"Tank Capacity (L)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 50, 100, 200, 300\"},{\"id\":\"maxPressureBar\",\"label\":\"Max Pressure (Bar / PSI)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 8 bar (116 psi) or 10 bar\"},{\"id\":\"motorPowerHp\",\"label\":\"Motor Power (HP)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2, 3, 5.5, 7.5\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Electric 230V (Single Phase)\",\"Electric 400V (Three Phase)\",\"Petrol Engine\",\"Diesel Engine\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Air Compressor", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-00000000021f"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Winget, Belle, Local Heavy Duty\"},{\"id\":\"drumCapacityL\",\"label\":\"Drum Capacity (Litres or Bags)\",\"type\":\"select\",\"required\":true,\"options\":[\"Half Bag (140L)\",\"1 Bag (200L - 250L)\",\"2 Bag (400L - 500L)\"]},{\"id\":\"powerSource\",\"label\":\"Engine / Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Diesel Engine (e.g. Yanmar/Kubota/Kirloskar)\",\"Petrol Engine (e.g. Honda)\",\"Electric Motor 230V\",\"Electric Motor 400V\"]},{\"id\":\"isTowable\",\"label\":\"Towable / Mobile\",\"type\":\"select\",\"required\":false,\"options\":[\"Yes (Pneumatic Wheels \\u0026 Towbar)\",\"Stationary / Site Wheels\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Concrete Mixer", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000220"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Honda, Koshin, Tsurumi\"},{\"id\":\"pumpType\",\"label\":\"Pump Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Self-Priming Centrifugal / Dewatering\",\"Submersible Sludge / Trash Pump\",\"High Pressure Fire / Irrigation Pump\"]},{\"id\":\"inletOutletSizeMm\",\"label\":\"Inlet/Outlet Size\",\"type\":\"select\",\"required\":true,\"options\":[\"2 inch (50mm)\",\"3 inch (75mm)\",\"4 inch (100mm)\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine\",\"Diesel Engine\",\"Electric 230V\",\"Electric 400V\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Water Pump", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000221"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Wacker Neuson, Mikasa, Bomag\"},{\"id\":\"compactorType\",\"label\":\"Compactor Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Forward Plate Compactor\",\"Reversible Plate Compactor\",\"Tamping Rammer (Jumping Jack)\",\"Walk-Behind Vibratory Roller\"]},{\"id\":\"operatingWeightKg\",\"label\":\"Operating Weight (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 70, 90, 160\"},{\"id\":\"engineType\",\"label\":\"Engine Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine (Honda GX)\",\"Diesel Engine (Hatz/Yanmar)\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Compactor", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000222"), new[] { 1m }, new[] { "piece", "unit" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false},{\"id\":\"needleDiameterMm\",\"label\":\"Needle Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"25mm\",\"35mm\",\"45mm\",\"50mm\",\"60mm\"]},{\"id\":\"shaftLengthM\",\"label\":\"Flexible Shaft Length (m)\",\"type\":\"select\",\"required\":true,\"options\":[\"4m\",\"5m\",\"6m\"]},{\"id\":\"driveUnit\",\"label\":\"Drive Unit\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine Drive\",\"Electric Motor Drive (230V)\",\"Handheld Portable Electric\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Concrete Vibrator", "PIECE", "PER_UNIT", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000223"), new[] { 1m }, new[] { "piece", "set" }, "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 3M DBI-SALA, Karam, Miller\"},{\"id\":\"certificationStandard\",\"label\":\"Safety Certification\",\"type\":\"select\",\"required\":true,\"options\":[\"EN 361 (CE Certified)\",\"ANSI Z359\",\"OSHA Compliant\"]},{\"id\":\"attachmentPoints\",\"label\":\"Attachment D-Rings\",\"type\":\"select\",\"required\":false,\"options\":[\"1-Point (Dorsal)\",\"2-Point (Dorsal \\u0026 Sternal)\",\"4-Point (Dorsal, Sternal, Work Positioning)\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Fall Protection Harness", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000224"), new[] { 1m }, new[] { "piece" }, "[{\"id\":\"standard\",\"label\":\"Safety Standard\",\"type\":\"select\",\"required\":true,\"options\":[\"EN 397 (Industrial)\",\"ANSI/ISEA Z89.1 Type 1 Class E\",\"SLS 614\"]},{\"id\":\"colour\",\"label\":\"Helmet Colour\",\"type\":\"select\",\"required\":true,\"options\":[\"White (Engineers/Managers)\",\"Yellow (General Labor)\",\"Blue (Electricians/Carpenters)\",\"Green (Safety Officers)\",\"Orange\",\"Red\"]},{\"id\":\"suspensionType\",\"label\":\"Suspension\",\"type\":\"select\",\"required\":false,\"options\":[\"Ratchet Wheel Adjustment\",\"Pin-Lock Adjustment\"]}]", "piece", new Guid("00000000-0000-0000-0000-00000000011b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EQUIPMENT", "Safety Helmet", "PIECE", "PER_PIECE", "PIECE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ConstructionItemTemplateId",
                table: "Listings",
                column: "ConstructionItemTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionItemTemplates_CategoryId",
                table: "ConstructionItemTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionItemTemplates_IsActive",
                table: "ConstructionItemTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionItemTemplates_ItemClass",
                table: "ConstructionItemTemplates",
                column: "ItemClass");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionItemTemplates_Name",
                table: "ConstructionItemTemplates",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_ConstructionItemTemplates_ConstructionItemTemplateId",
                table: "Listings",
                column: "ConstructionItemTemplateId",
                principalTable: "ConstructionItemTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_ConstructionItemTemplates_ConstructionItemTemplateId",
                table: "Listings");

            migrationBuilder.DropTable(
                name: "ConstructionItemTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ConstructionItemTemplateId",
                table: "Listings");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000113"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000114"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000116"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000117"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000119"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000011a"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000011b"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000011c"));

            migrationBuilder.DropColumn(
                name: "ConstructionItemTemplateId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsCustomPendingReview",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SpecificationsJson",
                table: "Listings");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "AllowedUnits",
                value: new[] { "pcs", "m", "m2", "m3", "kg", "g", "l", "bag", "box", "set", "roll", "sheet", "tonne", "pair" });
        }
    }
}
