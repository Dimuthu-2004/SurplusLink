using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSriLankanCatalogFormMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000099"),
                column: "AttributeSchema",
                value: "[{\"id\":\"itemName\",\"label\":\"Item Name\",\"type\":\"string\",\"required\":false,\"placeholder\":\"What construction item are you listing?\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Item Name\",\"si\":\"Item Name\",\"ta\":\"Item Name\"},\"helper\":\"\"},{\"id\":\"soldAs\",\"label\":\"Sold As\",\"type\":\"select\",\"required\":false,\"options\":[\"Individual piece / unit\",\"Package / container\",\"Continuous bulk / volume / weight\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Sold As\",\"si\":\"Sold As\",\"ta\":\"Sold As\"},\"helper\":\"\"},{\"id\":\"specifications\",\"label\":\"Specifications \\u0026 Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dimensions, material, grade, capacity\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Specifications \\u0026 Dimensions\",\"si\":\"Specifications \\u0026 Dimensions\",\"ta\":\"Specifications \\u0026 Dimensions\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dulux, Asian Paints, Nippon\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand / Manufacturer\",\"si\":\"Brand / Manufacturer\",\"ta\":\"Brand / Manufacturer\"},\"helper\":\"\"},{\"id\":\"paintType\",\"label\":\"Paint Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Emulsion\",\"Gloss / Enamel\",\"Weather-shield / Exterior\",\"Primer\",\"Undercoat\",\"Epoxy\",\"Other\"],\"priority\":\"REQUIRED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Paint Type\",\"si\":\"\\u0DAD\\u0DD3\\u0DB1\\u0DCA\\u0DAD \\u0DC0\\u0DBB\\u0DCA\\u0D9C\\u0DBA\",\"ta\":\"\\u0BAA\\u0BC6\\u0BAF\\u0BBF\\u0BA3\\u0BCD\\u0B9F\\u0BCD \\u0BB5\\u0B95\\u0BC8\"},\"helper\":\"\"},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Brilliant White, Cream, Slate Grey\",\"priority\":\"REQUIRED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Colour\",\"si\":\"Colour\",\"ta\":\"Colour\"},\"helper\":\"\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Matt\",\"Satin\",\"Semi-Gloss\",\"High Gloss\",\"Eggshell\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Finish\",\"si\":\"\\u0DB1\\u0DD2\\u0DB8\\u0DCF\\u0DC0\",\"ta\":\"\\u0BAA\\u0BC2\\u0B9A\\u0BCD\\u0B9A\\u0BC1\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Perkins, Cummins, Honda, Denyo\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand / Manufacturer\",\"si\":\"Brand / Manufacturer\",\"ta\":\"Brand / Manufacturer\"},\"helper\":\"\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. DG6500, EU22i\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Model\",\"si\":\"Model\",\"ta\":\"Model\"},\"helper\":\"\"},{\"id\":\"capacityKva\",\"label\":\"Capacity (kVA)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 5, 10, 50, 100\",\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":false,\"labelI18n\":{\"en\":\"Capacity (kVA)\",\"si\":\"\\u0DB0\\u0DCF\\u0DBB\\u0DD2\\u0DAD\\u0DCF\\u0DC0 (kVA)\",\"ta\":\"\\u0BA4\\u0BBF\\u0BB1\\u0BA9\\u0BCD (kVA)\"},\"helper\":\"Usually written on the generator label.\"},{\"id\":\"fuelType\",\"label\":\"Fuel Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Diesel\",\"Petrol\",\"Gas\",\"Dual Fuel\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Fuel Type\",\"si\":\"\\u0D89\\u0DB1\\u0DCA\\u0DB0\\u0DB1 \\u0DC0\\u0DBB\\u0DCA\\u0D9C\\u0DBA\",\"ta\":\"\\u0B8E\\u0BB0\\u0BBF\\u0BAA\\u0BCA\\u0BB0\\u0BC1\\u0BB3\\u0BCD \\u0BB5\\u0B95\\u0BC8\"},\"helper\":\"\"},{\"id\":\"phase\",\"label\":\"Phase\",\"type\":\"select\",\"required\":false,\"options\":[\"Single Phase\",\"Three Phase\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Phase\",\"si\":\"Phase\",\"ta\":\"Phase\"},\"helper\":\"\"},{\"id\":\"voltage\",\"label\":\"Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"230V\",\"400V\",\"110V/230V Dual\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Voltage\",\"si\":\"Voltage\",\"ta\":\"Voltage\"},\"helper\":\"\"},{\"id\":\"runningHours\",\"label\":\"Running Hours\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 450\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Running Hours\",\"si\":\"Running Hours\",\"ta\":\"Running Hours\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Tokyo Super, Insee Sanstha, Ultratech\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand / Manufacturer\",\"si\":\"Brand / Manufacturer\",\"ta\":\"Brand / Manufacturer\"},\"helper\":\"\"},{\"id\":\"cementType\",\"label\":\"Cement Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Ordinary Portland Cement (OPC)\",\"Portland Pozzolana Cement (PPC)\",\"Rapid Hardening\",\"White Cement\",\"Masonry Cement\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Cement Type\",\"si\":\"\\u0DC3\\u0DD2\\u0DB8\\u0DD9\\u0DB1\\u0DCA\\u0DAD\\u0DD2 \\u0DC0\\u0DBB\\u0DCA\\u0D9C\\u0DBA\",\"ta\":\"\\u0B9A\\u0BBF\\u0BAE\\u0BC6\\u0BA8\\u0BCD\\u0BA4\\u0BC1 \\u0BB5\\u0B95\\u0BC8\"},\"helper\":\"\"},{\"id\":\"grade\",\"label\":\"Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"Grade 43\",\"Grade 53\",\"Blended / Standard\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Grade\",\"si\":\"Grade\",\"ta\":\"Grade\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                column: "AttributeSchema",
                value: "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":false,\"options\":[\"6mm\",\"8mm\",\"10mm\",\"12mm\",\"16mm\",\"20mm\",\"25mm\",\"32mm\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Diameter (mm)\",\"si\":\"\\u0DC0\\u0DD2\\u0DC2\\u0DCA\\u0D9A\\u0DB8\\u0DCA\\u0DB7\\u0DBA (\\u0DB8\\u0DD2.\\u0DB8\\u0DD3.)\",\"ta\":\"\\u0BB5\\u0BBF\\u0B9F\\u0BCD\\u0B9F\\u0BAE\\u0BCD (\\u0BAE\\u0BBF\\u0BAE\\u0BC0)\"},\"helper\":\"Choose the diameter printed on the bar tag.\"},{\"id\":\"lengthM\",\"label\":\"Length per rod (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 6 or 12\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Length per rod (m)\",\"si\":\"Length per rod (m)\",\"ta\":\"Length per rod (m)\"},\"helper\":\"\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"RB500 / Grade 500\",\"RB415 / Grade 415\",\"Mild Steel Grade 250\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Steel Grade\",\"si\":\"Steel Grade\",\"ta\":\"Steel Grade\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sectionType\",\"label\":\"Section Type\",\"type\":\"select\",\"required\":false,\"options\":[\"I-Beam / Universal Beam\",\"H-Column\",\"C-Channel\",\"Equal Angle (L-section)\",\"Box Section / Hollow Tube (SHS/RHS)\",\"Flat Bar\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Section Type\",\"si\":\"Section Type\",\"ta\":\"Section Type\"},\"helper\":\"\"},{\"id\":\"dimensions\",\"label\":\"Dimensions / Spec\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 150x75mm, 100x50x4mm\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Dimensions / Spec\",\"si\":\"Dimensions / Spec\",\"ta\":\"Dimensions / Spec\"},\"helper\":\"\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 6.0\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Length (m)\",\"si\":\"Length (m)\",\"ta\":\"Length (m)\"},\"helper\":\"\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"S275\",\"S355\",\"Grade 43\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Steel Grade\",\"si\":\"Steel Grade\",\"ta\":\"Steel Grade\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000206"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brickType\",\"label\":\"Brick Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Red Clay Wire-Cut\",\"Traditional Burnt Clay\",\"Fly Ash\",\"Engineering Brick\",\"Refractory / Fire Brick\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Brick Type\",\"si\":\"\\u0D9C\\u0DA9\\u0DDC\\u0DBD\\u0DCA \\u0DC0\\u0DBB\\u0DCA\\u0D9C\\u0DBA\",\"ta\":\"\\u0B9A\\u0BC6\\u0B99\\u0BCD\\u0B95\\u0BB2\\u0BCD \\u0BB5\\u0B95\\u0BC8\"},\"helper\":\"\"},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 215 x 102.5 x 65\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":false,\"labelI18n\":{\"en\":\"Dimensions (mm)\",\"si\":\"\\u0DB4\\u0DCA\\u200D\\u0DBB\\u0DB8\\u0DCF\\u0DAB\\u0DBA (\\u0DB8\\u0DD2.\\u0DB8\\u0DD3.)\",\"ta\":\"\\u0B85\\u0BB3\\u0BB5\\u0BC1 (\\u0BAE\\u0BBF\\u0BAE\\u0BC0)\"},\"helper\":\"Choose the closest size. Select Other for a different size.\"},{\"id\":\"compressiveStrength\",\"label\":\"Compressive Strength (N/mm\\u00B2)\",\"type\":\"string\",\"required\":false,\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Compressive Strength (N/mm\\u00B2)\",\"si\":\"Compressive Strength (N/mm\\u00B2)\",\"ta\":\"Compressive Strength (N/mm\\u00B2)\"},\"helper\":\"Additional specification (optional).\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000207"),
                column: "AttributeSchema",
                value: "[{\"id\":\"blockType\",\"label\":\"Block Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Cement Solid Block\",\"Cement Hollow Block\",\"Autoclaved Aerated Concrete (AAC)\",\"Cellular Lightweight Concrete (CLC)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Block Type\",\"si\":\"Block Type\",\"ta\":\"Block Type\"},\"helper\":\"\"},{\"id\":\"thicknessInches\",\"label\":\"Thickness\",\"type\":\"select\",\"required\":false,\"options\":[\"4 inch\",\"6 inch\",\"8 inch\",\"9 inch\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Thickness\",\"si\":\"Thickness\",\"ta\":\"Thickness\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000208"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sandType\",\"label\":\"Sand Type\",\"type\":\"select\",\"required\":false,\"options\":[\"River Sand\",\"Manufactured Sand (M-Sand)\",\"Plastering Sand (P-Sand)\",\"Filling Sand\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Sand Type\",\"si\":\"Sand Type\",\"ta\":\"Sand Type\"},\"helper\":\"\"},{\"id\":\"screeningStatus\",\"label\":\"Screening\",\"type\":\"select\",\"required\":false,\"options\":[\"Screened / Washed\",\"Unscreened\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Screening\",\"si\":\"Screening\",\"ta\":\"Screening\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000209"),
                column: "AttributeSchema",
                value: "[{\"id\":\"aggregateSize\",\"label\":\"Aggregate Size\",\"type\":\"select\",\"required\":false,\"options\":[\"10mm (3/8\\u0022)\",\"20mm (3/4\\u0022)\",\"40mm (1.5\\u0022)\",\"ABC (Aggregate Base Coarse)\",\"Quarry Dust\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Aggregate Size\",\"si\":\"Aggregate Size\",\"ta\":\"Aggregate Size\"},\"helper\":\"\"},{\"id\":\"aggregateType\",\"label\":\"Source Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Crushed Granite Rock\",\"Limestone\",\"Gravel Aggregate\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Source Type\",\"si\":\"Source Type\",\"ta\":\"Source Type\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020a"),
                column: "AttributeSchema",
                value: "[{\"id\":\"gravelSize\",\"label\":\"Gravel Size\",\"type\":\"select\",\"required\":false,\"options\":[\"Fine (2-6mm)\",\"Medium (6-20mm)\",\"Coarse (20-60mm)\",\"Pea Gravel\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Gravel Size\",\"si\":\"Gravel Size\",\"ta\":\"Gravel Size\"},\"helper\":\"\"},{\"id\":\"washed\",\"label\":\"Washed / Clean\",\"type\":\"select\",\"required\":false,\"options\":[\"Washed\",\"Unwashed\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Washed / Clean\",\"si\":\"Washed / Clean\",\"ta\":\"Washed / Clean\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020b"),
                column: "AttributeSchema",
                value: "[{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":false,\"options\":[\"Ceramic\",\"Porcelain\",\"Granite\",\"Marble\",\"Terracotta\",\"Glass Mosaic\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Material\",\"si\":\"Material\",\"ta\":\"Material\"},\"helper\":\"\"},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 600x600, 300x300, 300x600\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":false,\"labelI18n\":{\"en\":\"Dimensions (mm)\",\"si\":\"\\u0DB4\\u0DCA\\u200D\\u0DBB\\u0DB8\\u0DCF\\u0DAB\\u0DBA (\\u0DB8\\u0DD2.\\u0DB8\\u0DD3.)\",\"ta\":\"\\u0B85\\u0BB3\\u0BB5\\u0BC1 (\\u0BAE\\u0BBF\\u0BAE\\u0BC0)\"},\"helper\":\"\"},{\"id\":\"piecesPerBox\",\"label\":\"Pieces per Box\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 4\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Pieces per Box\",\"si\":\"Pieces per Box\",\"ta\":\"Pieces per Box\"},\"helper\":\"Number of individual tiles inside one box.\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Polished / Gloss\",\"Matt\",\"Anti-Slip\",\"Textured / Rustic\",\"Honed\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Finish\",\"si\":\"\\u0DB1\\u0DD2\\u0DB8\\u0DCF\\u0DC0\",\"ta\":\"\\u0BAA\\u0BC2\\u0B9A\\u0BCD\\u0B9A\\u0BC1\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020c"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Weber, Laticrete, Bostik\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"adhesiveType\",\"label\":\"Adhesive Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Standard Cementitious (C1)\",\"Improved Flexible (C2TE)\",\"Epoxy Adhesive (R2)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Adhesive Type\",\"si\":\"Adhesive Type\",\"ta\":\"Adhesive Type\"},\"helper\":\"\"},{\"id\":\"coverageArea\",\"label\":\"Coverage Area (sqm/bag)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 5-7 sqm\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Coverage Area (sqm/bag)\",\"si\":\"Coverage Area (sqm/bag)\",\"ta\":\"Coverage Area (sqm/bag)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020d"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sealantType\",\"label\":\"Sealant Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Silicone (Acetic)\",\"Silicone (Neutral)\",\"Polyurethane (PU)\",\"Acrylic / Gap Filler\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Sealant Type\",\"si\":\"Sealant Type\",\"ta\":\"Sealant Type\"},\"helper\":\"\"},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Clear, White, Grey, Black\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Colour\",\"si\":\"Colour\",\"ta\":\"Colour\"},\"helper\":\"\"},{\"id\":\"volumeMl\",\"label\":\"Cartridge Size (ml)\",\"type\":\"select\",\"required\":false,\"options\":[\"300ml\",\"310ml\",\"600ml sausage\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Cartridge Size (ml)\",\"si\":\"Cartridge Size (ml)\",\"ta\":\"Cartridge Size (ml)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020e"),
                column: "AttributeSchema",
                value: "[{\"id\":\"waterproofingType\",\"label\":\"Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Liquid Applied Membrane\",\"Cementitious 2-Part System\",\"Bituminous Coating\",\"Polyurethane Coating\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Type\",\"si\":\"Type\",\"ta\":\"Type\"},\"helper\":\"\"},{\"id\":\"applicationArea\",\"label\":\"Recommended Area\",\"type\":\"select\",\"required\":false,\"options\":[\"Bathroom / Wet Areas\",\"Rooftop / Balcony\",\"Basement / Retaining Wall\",\"Water Tank\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Recommended Area\",\"si\":\"Recommended Area\",\"ta\":\"Recommended Area\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020f"),
                column: "AttributeSchema",
                value: "[{\"id\":\"woodSpecies\",\"label\":\"Species / Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Teak\",\"Mahogany\",\"Pine\",\"Kempas\",\"Treated Rubberwood\",\"Grandis\",\"Hardwood Mixed\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Species / Type\",\"si\":\"Species / Type\",\"ta\":\"Species / Type\"},\"helper\":\"\"},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 25, 50\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Thickness (mm)\",\"si\":\"Thickness (mm)\",\"ta\":\"Thickness (mm)\"},\"helper\":\"\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 50, 100, 150\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Width (mm)\",\"si\":\"Width (mm)\",\"ta\":\"Width (mm)\"},\"helper\":\"\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2.4, 3.0, 3.6\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Length (m)\",\"si\":\"Length (m)\",\"ta\":\"Length (m)\"},\"helper\":\"\"},{\"id\":\"treatment\",\"label\":\"Treatment\",\"type\":\"select\",\"required\":false,\"options\":[\"Kiln Dried \\u0026 Treated\",\"Air Dried\",\"Rough Sawn / Green\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Treatment\",\"si\":\"Treatment\",\"ta\":\"Treatment\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000210"),
                column: "AttributeSchema",
                value: "[{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":false,\"options\":[\"3mm\",\"6mm\",\"9mm\",\"12mm\",\"15mm\",\"18mm\",\"25mm\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Thickness (mm)\",\"si\":\"Thickness (mm)\",\"ta\":\"Thickness (mm)\"},\"helper\":\"\"},{\"id\":\"grade\",\"label\":\"Grade / Spec\",\"type\":\"select\",\"required\":false,\"options\":[\"Commercial / Interior Plywood\",\"Marine Grade (Waterproof)\",\"Film Faced / Shuttering Plywood\",\"BWP (Boiling Water Proof)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Grade / Spec\",\"si\":\"Grade / Spec\",\"ta\":\"Grade / Spec\"},\"helper\":\"\"},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions\",\"type\":\"select\",\"required\":false,\"options\":[\"8ft x 4ft (2440 x 1220 mm)\",\"7ft x 3ft\",\"6ft x 3ft\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Sheet Dimensions\",\"si\":\"Sheet Dimensions\",\"ta\":\"Sheet Dimensions\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000211"),
                column: "AttributeSchema",
                value: "[{\"id\":\"material\",\"label\":\"Material Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Zinc-Alum / Colorbond Steel\",\"Polycarbonate (Clear/Tinted)\",\"Asbestos-Free Fibre Cement\",\"UPVC Multi-Wall\",\"Transparent Acrylic\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Material Type\",\"si\":\"Material Type\",\"ta\":\"Material Type\"},\"helper\":\"\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2.4, 3.0, 3.6, 6.0\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Length (m)\",\"si\":\"Length (m)\",\"ta\":\"Length (m)\"},\"helper\":\"\"},{\"id\":\"widthM\",\"label\":\"Width (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1.0\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Width (m)\",\"si\":\"Width (m)\",\"ta\":\"Width (m)\"},\"helper\":\"\"},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 0.47, 0.50, 1.2\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Thickness (mm)\",\"si\":\"Thickness (mm)\",\"ta\":\"Thickness (mm)\"},\"helper\":\"\"},{\"id\":\"profile\",\"label\":\"Profile\",\"type\":\"select\",\"required\":false,\"options\":[\"Corrugated / Wave\",\"Trapezoidal / Box Rib\",\"Tile Look\",\"Standing Seam\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Profile\",\"si\":\"Profile\",\"ta\":\"Profile\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000212"),
                column: "AttributeSchema",
                value: "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":false,\"options\":[\"20mm (1/2\\u0022)\",\"25mm (3/4\\u0022)\",\"32mm (1\\u0022)\",\"40mm (1-1/4\\u0022)\",\"50mm (1-1/2\\u0022)\",\"63mm (2\\u0022)\",\"75mm (2-1/2\\u0022)\",\"90mm (3\\u0022)\",\"110mm (4\\u0022)\",\"160mm (6\\u0022)\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Diameter (mm)\",\"si\":\"\\u0DC0\\u0DD2\\u0DC2\\u0DCA\\u0D9A\\u0DB8\\u0DCA\\u0DB7\\u0DBA (\\u0DB8\\u0DD2.\\u0DB8\\u0DD3.)\",\"ta\":\"\\u0BB5\\u0BBF\\u0B9F\\u0BCD\\u0B9F\\u0BAE\\u0BCD (\\u0BAE\\u0BBF\\u0BAE\\u0BC0)\"},\"helper\":\"\"},{\"id\":\"lengthPerPieceM\",\"label\":\"Length per piece (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 4 or 6\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Length per piece (m)\",\"si\":\"Length per piece (m)\",\"ta\":\"Length per piece (m)\"},\"helper\":\"\"},{\"id\":\"pressureClass\",\"label\":\"Pressure Class\",\"type\":\"select\",\"required\":false,\"options\":[\"Type 400 (Low Pressure)\",\"Type 600 (Standard)\",\"Type 1000 (High Pressure)\",\"Non-Pressure / Drainage\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Pressure Class\",\"si\":\"Pressure Class\",\"ta\":\"Pressure Class\"},\"helper\":\"\"},{\"id\":\"pipeType\",\"label\":\"Pipe Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Potable Cold Water\",\"Drainage / Waste / Vent (DWV)\",\"Electrical Conduit\",\"Rainwater Downpipe\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Pipe Type\",\"si\":\"Pipe Type\",\"ta\":\"Pipe Type\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000213"),
                column: "AttributeSchema",
                value: "[{\"id\":\"conductorSizeMm2\",\"label\":\"Conductor Size\",\"type\":\"select\",\"required\":false,\"options\":[\"1.0 mm\\u00B2\",\"1.5 mm\\u00B2\",\"2.5 mm\\u00B2\",\"4.0 mm\\u00B2\",\"6.0 mm\\u00B2\",\"10.0 mm\\u00B2\",\"16.0 mm\\u00B2\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Conductor Size\",\"si\":\"Conductor Size\",\"ta\":\"Conductor Size\"},\"helper\":\"\"},{\"id\":\"coreCount\",\"label\":\"Cores\",\"type\":\"select\",\"required\":false,\"options\":[\"Single Core (1C)\",\"Twin \\u0026 Earth (2C\\u002BE)\",\"3 Core\",\"4 Core Armoured\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Cores\",\"si\":\"Cores\",\"ta\":\"Cores\"},\"helper\":\"\"},{\"id\":\"insulationType\",\"label\":\"Insulation\",\"type\":\"select\",\"required\":false,\"options\":[\"PVC\",\"XLPE\",\"LSZH (Low Smoke Zero Halogen)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Insulation\",\"si\":\"Insulation\",\"ta\":\"Insulation\"},\"helper\":\"\"},{\"id\":\"voltageRating\",\"label\":\"Voltage Rating\",\"type\":\"select\",\"required\":false,\"options\":[\"300/500V\",\"450/750V\",\"600/1000V\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Voltage Rating\",\"si\":\"Voltage Rating\",\"ta\":\"Voltage Rating\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000214"),
                column: "AttributeSchema",
                value: "[{\"id\":\"doorType\",\"label\":\"Door Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Solid Wood Panel Door\",\"Flush Door (Hollow Core)\",\"Flush Door (Solid Core)\",\"Aluminium Framed Glass Door\",\"UPVC Bathroom Door\",\"Steel Security Door\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Door Type\",\"si\":\"Door Type\",\"ta\":\"Door Type\"},\"helper\":\"\"},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2100\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Height (mm)\",\"si\":\"Height (mm)\",\"ta\":\"Height (mm)\"},\"helper\":\"\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 900, 800, 750\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Width (mm)\",\"si\":\"Width (mm)\",\"ta\":\"Width (mm)\"},\"helper\":\"\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Raw / Unfinished\",\"Stained \\u0026 Varnished\",\"Primed\",\"Painted\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Finish\",\"si\":\"\\u0DB1\\u0DD2\\u0DB8\\u0DCF\\u0DC0\",\"ta\":\"\\u0BAA\\u0BC2\\u0B9A\\u0BCD\\u0B9A\\u0BC1\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000215"),
                column: "AttributeSchema",
                value: "[{\"id\":\"frameMaterial\",\"label\":\"Frame Material\",\"type\":\"select\",\"required\":false,\"options\":[\"Aluminium (Powder Coated)\",\"Aluminium (Anodized)\",\"UPVC\",\"Timber\",\"Steel\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Frame Material\",\"si\":\"Frame Material\",\"ta\":\"Frame Material\"},\"helper\":\"\"},{\"id\":\"openingMechanism\",\"label\":\"Opening Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Sliding (2 Panel)\",\"Sliding (3/4 Panel)\",\"Casement\",\"Awning / Top Hung\",\"Fixed / Picture Window\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Opening Type\",\"si\":\"Opening Type\",\"ta\":\"Opening Type\"},\"helper\":\"\"},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1200\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Height (mm)\",\"si\":\"Height (mm)\",\"ta\":\"Height (mm)\"},\"helper\":\"\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Width (mm)\",\"si\":\"Width (mm)\",\"ta\":\"Width (mm)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000216"),
                column: "AttributeSchema",
                value: "[{\"id\":\"glassType\",\"label\":\"Glass Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Clear Float\",\"Toughened / Tempered\",\"Laminated Safety Glass\",\"Frosted / Obscure\",\"Tinted / Solar Reflective\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Glass Type\",\"si\":\"Glass Type\",\"ta\":\"Glass Type\"},\"helper\":\"\"},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":false,\"options\":[\"4mm\",\"5mm\",\"6mm\",\"8mm\",\"10mm\",\"12mm\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Thickness (mm)\",\"si\":\"Thickness (mm)\",\"ta\":\"Thickness (mm)\"},\"helper\":\"\"},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1830 x 1220\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Sheet Dimensions (mm)\",\"si\":\"Sheet Dimensions (mm)\",\"ta\":\"Sheet Dimensions (mm)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000217"),
                column: "AttributeSchema",
                value: "[{\"id\":\"scaffoldType\",\"label\":\"Scaffold Type\",\"type\":\"select\",\"required\":false,\"options\":[\"H-Frame Scaffolding Set\",\"Cuplock System\",\"Tube \\u0026 Coupler\",\"Mobile Tower Scaffold (Castor Wheels)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Scaffold Type\",\"si\":\"Scaffold Type\",\"ta\":\"Scaffold Type\"},\"helper\":\"\"},{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":false,\"options\":[\"Galvanized Steel\",\"Painted Steel\",\"Aluminium\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Material\",\"si\":\"Material\",\"ta\":\"Material\"},\"helper\":\"\"},{\"id\":\"componentsIncluded\",\"label\":\"Components Included\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 2 Frames, 2 Cross Braces, 4 Joint Pins\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Components Included\",\"si\":\"Components Included\",\"ta\":\"Components Included\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000218"),
                column: "AttributeSchema",
                value: "[{\"id\":\"formworkType\",\"label\":\"Formwork Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Steel Formwork Panels\",\"Aluminium Formwork Panels\",\"Film-Faced Shuttering Board\",\"Plastic Modular Formwork\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Formwork Type\",\"si\":\"Formwork Type\",\"ta\":\"Formwork Type\"},\"helper\":\"\"},{\"id\":\"dimensions\",\"label\":\"Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1200 x 600 mm\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Dimensions\",\"si\":\"Dimensions\",\"ta\":\"Dimensions\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000219"),
                column: "AttributeSchema",
                value: "[{\"id\":\"propType\",\"label\":\"Prop Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Acro Prop / Telescopic Steel Prop\",\"Heavy Duty Shoring Prop\",\"Push-Pull Prop\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Prop Type\",\"si\":\"Prop Type\",\"ta\":\"Prop Type\"},\"helper\":\"\"},{\"id\":\"extendedHeightM\",\"label\":\"Max Extended Height\",\"type\":\"select\",\"required\":false,\"options\":[\"No. 0 (1.0m - 1.8m)\",\"No. 1 (1.75m - 3.1m)\",\"No. 2 (2.0m - 3.4m)\",\"No. 3 (2.6m - 4.0m)\",\"No. 4 (3.2m - 4.9m)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Max Extended Height\",\"si\":\"Max Extended Height\",\"ta\":\"Max Extended Height\"},\"helper\":\"\"},{\"id\":\"safeWorkingLoadKg\",\"label\":\"Safe Working Load (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Safe Working Load (kg)\",\"si\":\"Safe Working Load (kg)\",\"ta\":\"Safe Working Load (kg)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021a"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Bosch, Makita, DeWalt, DongCheng\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. GBH 2-26 DRE\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Model\",\"si\":\"Model\",\"ta\":\"Model\"},\"helper\":\"\"},{\"id\":\"drillType\",\"label\":\"Drill Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Rotary Hammer Drill (SDS-Plus)\",\"Heavy Demolition Hammer (SDS-Max)\",\"Impact Drill\",\"Cordless Combi Drill\",\"Other\"],\"priority\":\"RECOMMENDED\",\"sellerField\":true,\"buyerPreference\":true,\"allowOther\":true,\"labelI18n\":{\"en\":\"Drill Type\",\"si\":\"Drill Type\",\"ta\":\"Drill Type\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Corded 230V Electric\",\"Cordless 18V/20V Battery\",\"Cordless 36V/40V Max\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Power Source\",\"si\":\"Power Source\",\"ta\":\"Power Source\"},\"helper\":\"\"},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 800\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Wattage (W)\",\"si\":\"Wattage (W)\",\"ta\":\"Wattage (W)\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021b"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Bosch, Makita, DeWalt\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Model\",\"si\":\"Model\",\"ta\":\"Model\"},\"helper\":\"\"},{\"id\":\"discDiameterMm\",\"label\":\"Disc Diameter\",\"type\":\"select\",\"required\":false,\"options\":[\"100mm (4 inch)\",\"115mm (4.5 inch)\",\"125mm (5 inch)\",\"180mm (7 inch)\",\"230mm (9 inch)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Disc Diameter\",\"si\":\"Disc Diameter\",\"ta\":\"Disc Diameter\"},\"helper\":\"\"},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 850, 2200\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Wattage (W)\",\"si\":\"Wattage (W)\",\"ta\":\"Wattage (W)\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Corded 230V\",\"Cordless 18V/20V Battery\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Power Source\",\"si\":\"Power Source\",\"ta\":\"Power Source\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021c"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Makita, Bosch, DeWalt\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Model\",\"si\":\"Model\",\"ta\":\"Model\"},\"helper\":\"\"},{\"id\":\"bladeDiameterMm\",\"label\":\"Blade Diameter\",\"type\":\"select\",\"required\":false,\"options\":[\"165mm (6.5\\u0022)\",\"185mm (7.25\\u0022)\",\"235mm (9.25\\u0022)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Blade Diameter\",\"si\":\"Blade Diameter\",\"ta\":\"Blade Diameter\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Corded 230V\",\"Cordless 18V/40V Battery\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Power Source\",\"si\":\"Power Source\",\"ta\":\"Power Source\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021d"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Jasic, Riland, Miller, Lincoln\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"weldingType\",\"label\":\"Welding Process\",\"type\":\"select\",\"required\":false,\"options\":[\"MMA / Inverter Arc Welder\",\"MIG / MAG (Gas \\u0026 Gasless)\",\"TIG Welder\",\"Multi-Process MIG/TIG/MMA\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Welding Process\",\"si\":\"Welding Process\",\"ta\":\"Welding Process\"},\"helper\":\"\"},{\"id\":\"currentAmps\",\"label\":\"Max Current (Amps)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 160, 200, 250, 400\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Max Current (Amps)\",\"si\":\"Max Current (Amps)\",\"ta\":\"Max Current (Amps)\"},\"helper\":\"\"},{\"id\":\"inputVoltage\",\"label\":\"Input Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"Single Phase 230V\",\"Three Phase 400V\",\"Dual Voltage 230V/400V\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Input Voltage\",\"si\":\"Input Voltage\",\"ta\":\"Input Voltage\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021e"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Puma, Atlas Copco, Ingersoll Rand\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Model\",\"si\":\"Model\",\"ta\":\"Model\"},\"helper\":\"\"},{\"id\":\"tankCapacityL\",\"label\":\"Tank Capacity (L)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 50, 100, 200, 300\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Tank Capacity (L)\",\"si\":\"Tank Capacity (L)\",\"ta\":\"Tank Capacity (L)\"},\"helper\":\"\"},{\"id\":\"maxPressureBar\",\"label\":\"Max Pressure (Bar / PSI)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 8 bar (116 psi) or 10 bar\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Max Pressure (Bar / PSI)\",\"si\":\"Max Pressure (Bar / PSI)\",\"ta\":\"Max Pressure (Bar / PSI)\"},\"helper\":\"\"},{\"id\":\"motorPowerHp\",\"label\":\"Motor Power (HP)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2, 3, 5.5, 7.5\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Motor Power (HP)\",\"si\":\"Motor Power (HP)\",\"ta\":\"Motor Power (HP)\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Electric 230V (Single Phase)\",\"Electric 400V (Three Phase)\",\"Petrol Engine\",\"Diesel Engine\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Power Source\",\"si\":\"Power Source\",\"ta\":\"Power Source\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021f"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Winget, Belle, Local Heavy Duty\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"drumCapacityL\",\"label\":\"Drum Capacity (Litres or Bags)\",\"type\":\"select\",\"required\":false,\"options\":[\"Half Bag (140L)\",\"1 Bag (200L - 250L)\",\"2 Bag (400L - 500L)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Drum Capacity (Litres or Bags)\",\"si\":\"Drum Capacity (Litres or Bags)\",\"ta\":\"Drum Capacity (Litres or Bags)\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Engine / Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Diesel Engine (e.g. Yanmar/Kubota/Kirloskar)\",\"Petrol Engine (e.g. Honda)\",\"Electric Motor 230V\",\"Electric Motor 400V\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Engine / Power Source\",\"si\":\"Engine / Power Source\",\"ta\":\"Engine / Power Source\"},\"helper\":\"\"},{\"id\":\"isTowable\",\"label\":\"Towable / Mobile\",\"type\":\"select\",\"required\":false,\"options\":[\"Yes (Pneumatic Wheels \\u0026 Towbar)\",\"Stationary / Site Wheels\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Towable / Mobile\",\"si\":\"Towable / Mobile\",\"ta\":\"Towable / Mobile\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000220"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Honda, Koshin, Tsurumi\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"pumpType\",\"label\":\"Pump Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Self-Priming Centrifugal / Dewatering\",\"Submersible Sludge / Trash Pump\",\"High Pressure Fire / Irrigation Pump\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Pump Type\",\"si\":\"Pump Type\",\"ta\":\"Pump Type\"},\"helper\":\"\"},{\"id\":\"inletOutletSizeMm\",\"label\":\"Inlet/Outlet Size\",\"type\":\"select\",\"required\":false,\"options\":[\"2 inch (50mm)\",\"3 inch (75mm)\",\"4 inch (100mm)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Inlet/Outlet Size\",\"si\":\"Inlet/Outlet Size\",\"ta\":\"Inlet/Outlet Size\"},\"helper\":\"\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":false,\"options\":[\"Petrol Engine\",\"Diesel Engine\",\"Electric 230V\",\"Electric 400V\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Power Source\",\"si\":\"Power Source\",\"ta\":\"Power Source\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000221"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Wacker Neuson, Mikasa, Bomag\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"compactorType\",\"label\":\"Compactor Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Forward Plate Compactor\",\"Reversible Plate Compactor\",\"Tamping Rammer (Jumping Jack)\",\"Walk-Behind Vibratory Roller\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Compactor Type\",\"si\":\"Compactor Type\",\"ta\":\"Compactor Type\"},\"helper\":\"\"},{\"id\":\"operatingWeightKg\",\"label\":\"Operating Weight (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 70, 90, 160\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Operating Weight (kg)\",\"si\":\"Operating Weight (kg)\",\"ta\":\"Operating Weight (kg)\"},\"helper\":\"\"},{\"id\":\"engineType\",\"label\":\"Engine Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Petrol Engine (Honda GX)\",\"Diesel Engine (Hatz/Yanmar)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Engine Type\",\"si\":\"Engine Type\",\"ta\":\"Engine Type\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000222"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"needleDiameterMm\",\"label\":\"Needle Diameter (mm)\",\"type\":\"select\",\"required\":false,\"options\":[\"25mm\",\"35mm\",\"45mm\",\"50mm\",\"60mm\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Needle Diameter (mm)\",\"si\":\"Needle Diameter (mm)\",\"ta\":\"Needle Diameter (mm)\"},\"helper\":\"\"},{\"id\":\"shaftLengthM\",\"label\":\"Flexible Shaft Length (m)\",\"type\":\"select\",\"required\":false,\"options\":[\"4m\",\"5m\",\"6m\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Flexible Shaft Length (m)\",\"si\":\"Flexible Shaft Length (m)\",\"ta\":\"Flexible Shaft Length (m)\"},\"helper\":\"\"},{\"id\":\"driveUnit\",\"label\":\"Drive Unit\",\"type\":\"select\",\"required\":false,\"options\":[\"Petrol Engine Drive\",\"Electric Motor Drive (230V)\",\"Handheld Portable Electric\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Drive Unit\",\"si\":\"Drive Unit\",\"ta\":\"Drive Unit\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000223"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 3M DBI-SALA, Karam, Miller\",\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":false,\"labelI18n\":{\"en\":\"Brand\",\"si\":\"Brand\",\"ta\":\"Brand\"},\"helper\":\"\"},{\"id\":\"certificationStandard\",\"label\":\"Safety Certification\",\"type\":\"select\",\"required\":false,\"options\":[\"EN 361 (CE Certified)\",\"ANSI Z359\",\"OSHA Compliant\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Safety Certification\",\"si\":\"Safety Certification\",\"ta\":\"Safety Certification\"},\"helper\":\"\"},{\"id\":\"attachmentPoints\",\"label\":\"Attachment D-Rings\",\"type\":\"select\",\"required\":false,\"options\":[\"1-Point (Dorsal)\",\"2-Point (Dorsal \\u0026 Sternal)\",\"4-Point (Dorsal, Sternal, Work Positioning)\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Attachment D-Rings\",\"si\":\"Attachment D-Rings\",\"ta\":\"Attachment D-Rings\"},\"helper\":\"\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000224"),
                column: "AttributeSchema",
                value: "[{\"id\":\"standard\",\"label\":\"Safety Standard\",\"type\":\"select\",\"required\":false,\"options\":[\"EN 397 (Industrial)\",\"ANSI/ISEA Z89.1 Type 1 Class E\",\"SLS 614\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Safety Standard\",\"si\":\"Safety Standard\",\"ta\":\"Safety Standard\"},\"helper\":\"\"},{\"id\":\"colour\",\"label\":\"Helmet Colour\",\"type\":\"select\",\"required\":false,\"options\":[\"White (Engineers/Managers)\",\"Yellow (General Labor)\",\"Blue (Electricians/Carpenters)\",\"Green (Safety Officers)\",\"Orange\",\"Red\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Helmet Colour\",\"si\":\"Helmet Colour\",\"ta\":\"Helmet Colour\"},\"helper\":\"\"},{\"id\":\"suspensionType\",\"label\":\"Suspension\",\"type\":\"select\",\"required\":false,\"options\":[\"Ratchet Wheel Adjustment\",\"Pin-Lock Adjustment\",\"Other\"],\"priority\":\"OPTIONAL\",\"sellerField\":true,\"buyerPreference\":false,\"allowOther\":true,\"labelI18n\":{\"en\":\"Suspension\",\"si\":\"Suspension\",\"ta\":\"Suspension\"},\"helper\":\"\"}]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000099"),
                column: "AttributeSchema",
                value: "[{\"id\":\"itemName\",\"label\":\"Item Name\",\"type\":\"string\",\"required\":true,\"placeholder\":\"What construction item are you listing?\"},{\"id\":\"soldAs\",\"label\":\"Sold As\",\"type\":\"select\",\"required\":true,\"options\":[\"Individual piece / unit\",\"Package / container\",\"Continuous bulk / volume / weight\"]},{\"id\":\"specifications\",\"label\":\"Specifications \\u0026 Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dimensions, material, grade, capacity\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Dulux, Asian Paints, Nippon\"},{\"id\":\"paintType\",\"label\":\"Paint Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Emulsion\",\"Gloss / Enamel\",\"Weather-shield / Exterior\",\"Primer\",\"Undercoat\",\"Epoxy\"]},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Brilliant White, Cream, Slate Grey\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Matt\",\"Satin\",\"Semi-Gloss\",\"High Gloss\",\"Eggshell\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Perkins, Cummins, Honda, Denyo\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. DG6500, EU22i\"},{\"id\":\"capacityKva\",\"label\":\"Capacity (kVA)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 5, 10, 50, 100\"},{\"id\":\"fuelType\",\"label\":\"Fuel Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Diesel\",\"Petrol\",\"Gas\",\"Dual Fuel\"]},{\"id\":\"phase\",\"label\":\"Phase\",\"type\":\"select\",\"required\":true,\"options\":[\"Single Phase\",\"Three Phase\"]},{\"id\":\"voltage\",\"label\":\"Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"230V\",\"400V\",\"110V/230V Dual\"]},{\"id\":\"runningHours\",\"label\":\"Running Hours\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 450\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand / Manufacturer\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Tokyo Super, Insee Sanstha, Ultratech\"},{\"id\":\"cementType\",\"label\":\"Cement Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Ordinary Portland Cement (OPC)\",\"Portland Pozzolana Cement (PPC)\",\"Rapid Hardening\",\"White Cement\",\"Masonry Cement\"]},{\"id\":\"grade\",\"label\":\"Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"Grade 43\",\"Grade 53\",\"Blended / Standard\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                column: "AttributeSchema",
                value: "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"6mm\",\"8mm\",\"10mm\",\"12mm\",\"16mm\",\"20mm\",\"25mm\",\"32mm\"]},{\"id\":\"lengthM\",\"label\":\"Length per rod (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 6 or 12\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":true,\"options\":[\"RB500 / Grade 500\",\"RB415 / Grade 415\",\"Mild Steel Grade 250\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sectionType\",\"label\":\"Section Type\",\"type\":\"select\",\"required\":true,\"options\":[\"I-Beam / Universal Beam\",\"H-Column\",\"C-Channel\",\"Equal Angle (L-section)\",\"Box Section / Hollow Tube (SHS/RHS)\",\"Flat Bar\"]},{\"id\":\"dimensions\",\"label\":\"Dimensions / Spec\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. 150x75mm, 100x50x4mm\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 6.0\"},{\"id\":\"grade\",\"label\":\"Steel Grade\",\"type\":\"select\",\"required\":false,\"options\":[\"S275\",\"S355\",\"Grade 43\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000206"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brickType\",\"label\":\"Brick Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Red Clay Wire-Cut\",\"Traditional Burnt Clay\",\"Fly Ash\",\"Engineering Brick\",\"Refractory / Fire Brick\"]},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 215 x 102.5 x 65\"},{\"id\":\"compressiveStrength\",\"label\":\"Compressive Strength (N/mm\\u00B2)\",\"type\":\"string\",\"required\":false}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000207"),
                column: "AttributeSchema",
                value: "[{\"id\":\"blockType\",\"label\":\"Block Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Cement Solid Block\",\"Cement Hollow Block\",\"Autoclaved Aerated Concrete (AAC)\",\"Cellular Lightweight Concrete (CLC)\"]},{\"id\":\"thicknessInches\",\"label\":\"Thickness\",\"type\":\"select\",\"required\":true,\"options\":[\"4 inch\",\"6 inch\",\"8 inch\",\"9 inch\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000208"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sandType\",\"label\":\"Sand Type\",\"type\":\"select\",\"required\":true,\"options\":[\"River Sand\",\"Manufactured Sand (M-Sand)\",\"Plastering Sand (P-Sand)\",\"Filling Sand\"]},{\"id\":\"screeningStatus\",\"label\":\"Screening\",\"type\":\"select\",\"required\":false,\"options\":[\"Screened / Washed\",\"Unscreened\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000209"),
                column: "AttributeSchema",
                value: "[{\"id\":\"aggregateSize\",\"label\":\"Aggregate Size\",\"type\":\"select\",\"required\":true,\"options\":[\"10mm (3/8\\u0022)\",\"20mm (3/4\\u0022)\",\"40mm (1.5\\u0022)\",\"ABC (Aggregate Base Coarse)\",\"Quarry Dust\"]},{\"id\":\"aggregateType\",\"label\":\"Source Type\",\"type\":\"select\",\"required\":false,\"options\":[\"Crushed Granite Rock\",\"Limestone\",\"Gravel Aggregate\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020a"),
                column: "AttributeSchema",
                value: "[{\"id\":\"gravelSize\",\"label\":\"Gravel Size\",\"type\":\"select\",\"required\":true,\"options\":[\"Fine (2-6mm)\",\"Medium (6-20mm)\",\"Coarse (20-60mm)\",\"Pea Gravel\"]},{\"id\":\"washed\",\"label\":\"Washed / Clean\",\"type\":\"select\",\"required\":false,\"options\":[\"Washed\",\"Unwashed\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020b"),
                column: "AttributeSchema",
                value: "[{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":true,\"options\":[\"Ceramic\",\"Porcelain\",\"Granite\",\"Marble\",\"Terracotta\",\"Glass Mosaic\"]},{\"id\":\"dimensionsMm\",\"label\":\"Dimensions (mm)\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. 600x600, 300x300, 300x600\"},{\"id\":\"piecesPerBox\",\"label\":\"Pieces per Box\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 4\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Polished / Gloss\",\"Matt\",\"Anti-Slip\",\"Textured / Rustic\",\"Honed\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020c"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Weber, Laticrete, Bostik\"},{\"id\":\"adhesiveType\",\"label\":\"Adhesive Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Standard Cementitious (C1)\",\"Improved Flexible (C2TE)\",\"Epoxy Adhesive (R2)\"]},{\"id\":\"coverageArea\",\"label\":\"Coverage Area (sqm/bag)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 5-7 sqm\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020d"),
                column: "AttributeSchema",
                value: "[{\"id\":\"sealantType\",\"label\":\"Sealant Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Silicone (Acetic)\",\"Silicone (Neutral)\",\"Polyurethane (PU)\",\"Acrylic / Gap Filler\"]},{\"id\":\"colour\",\"label\":\"Colour\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Clear, White, Grey, Black\"},{\"id\":\"volumeMl\",\"label\":\"Cartridge Size (ml)\",\"type\":\"select\",\"required\":false,\"options\":[\"300ml\",\"310ml\",\"600ml sausage\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020e"),
                column: "AttributeSchema",
                value: "[{\"id\":\"waterproofingType\",\"label\":\"Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Liquid Applied Membrane\",\"Cementitious 2-Part System\",\"Bituminous Coating\",\"Polyurethane Coating\"]},{\"id\":\"applicationArea\",\"label\":\"Recommended Area\",\"type\":\"select\",\"required\":false,\"options\":[\"Bathroom / Wet Areas\",\"Rooftop / Balcony\",\"Basement / Retaining Wall\",\"Water Tank\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020f"),
                column: "AttributeSchema",
                value: "[{\"id\":\"woodSpecies\",\"label\":\"Species / Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Teak\",\"Mahogany\",\"Pine\",\"Kempas\",\"Treated Rubberwood\",\"Grandis\",\"Hardwood Mixed\"]},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 25, 50\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 50, 100, 150\"},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2.4, 3.0, 3.6\"},{\"id\":\"treatment\",\"label\":\"Treatment\",\"type\":\"select\",\"required\":false,\"options\":[\"Kiln Dried \\u0026 Treated\",\"Air Dried\",\"Rough Sawn / Green\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000210"),
                column: "AttributeSchema",
                value: "[{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"3mm\",\"6mm\",\"9mm\",\"12mm\",\"15mm\",\"18mm\",\"25mm\"]},{\"id\":\"grade\",\"label\":\"Grade / Spec\",\"type\":\"select\",\"required\":true,\"options\":[\"Commercial / Interior Plywood\",\"Marine Grade (Waterproof)\",\"Film Faced / Shuttering Plywood\",\"BWP (Boiling Water Proof)\"]},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions\",\"type\":\"select\",\"required\":false,\"options\":[\"8ft x 4ft (2440 x 1220 mm)\",\"7ft x 3ft\",\"6ft x 3ft\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000211"),
                column: "AttributeSchema",
                value: "[{\"id\":\"material\",\"label\":\"Material Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Zinc-Alum / Colorbond Steel\",\"Polycarbonate (Clear/Tinted)\",\"Asbestos-Free Fibre Cement\",\"UPVC Multi-Wall\",\"Transparent Acrylic\"]},{\"id\":\"lengthM\",\"label\":\"Length (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2.4, 3.0, 3.6, 6.0\"},{\"id\":\"widthM\",\"label\":\"Width (m)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1.0\"},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 0.47, 0.50, 1.2\"},{\"id\":\"profile\",\"label\":\"Profile\",\"type\":\"select\",\"required\":false,\"options\":[\"Corrugated / Wave\",\"Trapezoidal / Box Rib\",\"Tile Look\",\"Standing Seam\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000212"),
                column: "AttributeSchema",
                value: "[{\"id\":\"diameterMm\",\"label\":\"Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"20mm (1/2\\u0022)\",\"25mm (3/4\\u0022)\",\"32mm (1\\u0022)\",\"40mm (1-1/4\\u0022)\",\"50mm (1-1/2\\u0022)\",\"63mm (2\\u0022)\",\"75mm (2-1/2\\u0022)\",\"90mm (3\\u0022)\",\"110mm (4\\u0022)\",\"160mm (6\\u0022)\"]},{\"id\":\"lengthPerPieceM\",\"label\":\"Length per piece (m)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 4 or 6\"},{\"id\":\"pressureClass\",\"label\":\"Pressure Class\",\"type\":\"select\",\"required\":false,\"options\":[\"Type 400 (Low Pressure)\",\"Type 600 (Standard)\",\"Type 1000 (High Pressure)\",\"Non-Pressure / Drainage\"]},{\"id\":\"pipeType\",\"label\":\"Pipe Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Potable Cold Water\",\"Drainage / Waste / Vent (DWV)\",\"Electrical Conduit\",\"Rainwater Downpipe\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000213"),
                column: "AttributeSchema",
                value: "[{\"id\":\"conductorSizeMm2\",\"label\":\"Conductor Size\",\"type\":\"select\",\"required\":true,\"options\":[\"1.0 mm\\u00B2\",\"1.5 mm\\u00B2\",\"2.5 mm\\u00B2\",\"4.0 mm\\u00B2\",\"6.0 mm\\u00B2\",\"10.0 mm\\u00B2\",\"16.0 mm\\u00B2\"]},{\"id\":\"coreCount\",\"label\":\"Cores\",\"type\":\"select\",\"required\":true,\"options\":[\"Single Core (1C)\",\"Twin \\u0026 Earth (2C\\u002BE)\",\"3 Core\",\"4 Core Armoured\"]},{\"id\":\"insulationType\",\"label\":\"Insulation\",\"type\":\"select\",\"required\":false,\"options\":[\"PVC\",\"XLPE\",\"LSZH (Low Smoke Zero Halogen)\"]},{\"id\":\"voltageRating\",\"label\":\"Voltage Rating\",\"type\":\"select\",\"required\":false,\"options\":[\"300/500V\",\"450/750V\",\"600/1000V\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000214"),
                column: "AttributeSchema",
                value: "[{\"id\":\"doorType\",\"label\":\"Door Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Solid Wood Panel Door\",\"Flush Door (Hollow Core)\",\"Flush Door (Solid Core)\",\"Aluminium Framed Glass Door\",\"UPVC Bathroom Door\",\"Steel Security Door\"]},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 2100\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 900, 800, 750\"},{\"id\":\"finish\",\"label\":\"Finish\",\"type\":\"select\",\"required\":false,\"options\":[\"Raw / Unfinished\",\"Stained \\u0026 Varnished\",\"Primed\",\"Painted\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000215"),
                column: "AttributeSchema",
                value: "[{\"id\":\"frameMaterial\",\"label\":\"Frame Material\",\"type\":\"select\",\"required\":true,\"options\":[\"Aluminium (Powder Coated)\",\"Aluminium (Anodized)\",\"UPVC\",\"Timber\",\"Steel\"]},{\"id\":\"openingMechanism\",\"label\":\"Opening Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Sliding (2 Panel)\",\"Sliding (3/4 Panel)\",\"Casement\",\"Awning / Top Hung\",\"Fixed / Picture Window\"]},{\"id\":\"heightMm\",\"label\":\"Height (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1200\"},{\"id\":\"widthMm\",\"label\":\"Width (mm)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000216"),
                column: "AttributeSchema",
                value: "[{\"id\":\"glassType\",\"label\":\"Glass Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Clear Float\",\"Toughened / Tempered\",\"Laminated Safety Glass\",\"Frosted / Obscure\",\"Tinted / Solar Reflective\"]},{\"id\":\"thicknessMm\",\"label\":\"Thickness (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"4mm\",\"5mm\",\"6mm\",\"8mm\",\"10mm\",\"12mm\"]},{\"id\":\"dimensions\",\"label\":\"Sheet Dimensions (mm)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1830 x 1220\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000217"),
                column: "AttributeSchema",
                value: "[{\"id\":\"scaffoldType\",\"label\":\"Scaffold Type\",\"type\":\"select\",\"required\":true,\"options\":[\"H-Frame Scaffolding Set\",\"Cuplock System\",\"Tube \\u0026 Coupler\",\"Mobile Tower Scaffold (Castor Wheels)\"]},{\"id\":\"material\",\"label\":\"Material\",\"type\":\"select\",\"required\":false,\"options\":[\"Galvanized Steel\",\"Painted Steel\",\"Aluminium\"]},{\"id\":\"componentsIncluded\",\"label\":\"Components Included\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 2 Frames, 2 Cross Braces, 4 Joint Pins\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000218"),
                column: "AttributeSchema",
                value: "[{\"id\":\"formworkType\",\"label\":\"Formwork Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Steel Formwork Panels\",\"Aluminium Formwork Panels\",\"Film-Faced Shuttering Board\",\"Plastic Modular Formwork\"]},{\"id\":\"dimensions\",\"label\":\"Dimensions\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 1200 x 600 mm\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000219"),
                column: "AttributeSchema",
                value: "[{\"id\":\"propType\",\"label\":\"Prop Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Acro Prop / Telescopic Steel Prop\",\"Heavy Duty Shoring Prop\",\"Push-Pull Prop\"]},{\"id\":\"extendedHeightM\",\"label\":\"Max Extended Height\",\"type\":\"select\",\"required\":true,\"options\":[\"No. 0 (1.0m - 1.8m)\",\"No. 1 (1.75m - 3.1m)\",\"No. 2 (2.0m - 3.4m)\",\"No. 3 (2.6m - 4.0m)\",\"No. 4 (3.2m - 4.9m)\"]},{\"id\":\"safeWorkingLoadKg\",\"label\":\"Safe Working Load (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 1500\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021a"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Bosch, Makita, DeWalt, DongCheng\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. GBH 2-26 DRE\"},{\"id\":\"drillType\",\"label\":\"Drill Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Rotary Hammer Drill (SDS-Plus)\",\"Heavy Demolition Hammer (SDS-Max)\",\"Impact Drill\",\"Cordless Combi Drill\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V Electric\",\"Cordless 18V/20V Battery\",\"Cordless 36V/40V Max\"]},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 800\"}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021b"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Bosch, Makita, DeWalt\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"discDiameterMm\",\"label\":\"Disc Diameter\",\"type\":\"select\",\"required\":true,\"options\":[\"100mm (4 inch)\",\"115mm (4.5 inch)\",\"125mm (5 inch)\",\"180mm (7 inch)\",\"230mm (9 inch)\"]},{\"id\":\"wattage\",\"label\":\"Wattage (W)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 850, 2200\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V\",\"Cordless 18V/20V Battery\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021c"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Makita, Bosch, DeWalt\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"bladeDiameterMm\",\"label\":\"Blade Diameter\",\"type\":\"select\",\"required\":true,\"options\":[\"165mm (6.5\\u0022)\",\"185mm (7.25\\u0022)\",\"235mm (9.25\\u0022)\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Corded 230V\",\"Cordless 18V/40V Battery\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021d"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Jasic, Riland, Miller, Lincoln\"},{\"id\":\"weldingType\",\"label\":\"Welding Process\",\"type\":\"select\",\"required\":true,\"options\":[\"MMA / Inverter Arc Welder\",\"MIG / MAG (Gas \\u0026 Gasless)\",\"TIG Welder\",\"Multi-Process MIG/TIG/MMA\"]},{\"id\":\"currentAmps\",\"label\":\"Max Current (Amps)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 160, 200, 250, 400\"},{\"id\":\"inputVoltage\",\"label\":\"Input Voltage\",\"type\":\"select\",\"required\":false,\"options\":[\"Single Phase 230V\",\"Three Phase 400V\",\"Dual Voltage 230V/400V\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021e"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":true,\"placeholder\":\"e.g. Puma, Atlas Copco, Ingersoll Rand\"},{\"id\":\"model\",\"label\":\"Model\",\"type\":\"string\",\"required\":false},{\"id\":\"tankCapacityL\",\"label\":\"Tank Capacity (L)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 50, 100, 200, 300\"},{\"id\":\"maxPressureBar\",\"label\":\"Max Pressure (Bar / PSI)\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 8 bar (116 psi) or 10 bar\"},{\"id\":\"motorPowerHp\",\"label\":\"Motor Power (HP)\",\"type\":\"number\",\"required\":true,\"placeholder\":\"e.g. 2, 3, 5.5, 7.5\"},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Electric 230V (Single Phase)\",\"Electric 400V (Three Phase)\",\"Petrol Engine\",\"Diesel Engine\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021f"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Winget, Belle, Local Heavy Duty\"},{\"id\":\"drumCapacityL\",\"label\":\"Drum Capacity (Litres or Bags)\",\"type\":\"select\",\"required\":true,\"options\":[\"Half Bag (140L)\",\"1 Bag (200L - 250L)\",\"2 Bag (400L - 500L)\"]},{\"id\":\"powerSource\",\"label\":\"Engine / Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Diesel Engine (e.g. Yanmar/Kubota/Kirloskar)\",\"Petrol Engine (e.g. Honda)\",\"Electric Motor 230V\",\"Electric Motor 400V\"]},{\"id\":\"isTowable\",\"label\":\"Towable / Mobile\",\"type\":\"select\",\"required\":false,\"options\":[\"Yes (Pneumatic Wheels \\u0026 Towbar)\",\"Stationary / Site Wheels\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000220"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Honda, Koshin, Tsurumi\"},{\"id\":\"pumpType\",\"label\":\"Pump Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Self-Priming Centrifugal / Dewatering\",\"Submersible Sludge / Trash Pump\",\"High Pressure Fire / Irrigation Pump\"]},{\"id\":\"inletOutletSizeMm\",\"label\":\"Inlet/Outlet Size\",\"type\":\"select\",\"required\":true,\"options\":[\"2 inch (50mm)\",\"3 inch (75mm)\",\"4 inch (100mm)\"]},{\"id\":\"powerSource\",\"label\":\"Power Source\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine\",\"Diesel Engine\",\"Electric 230V\",\"Electric 400V\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000221"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. Wacker Neuson, Mikasa, Bomag\"},{\"id\":\"compactorType\",\"label\":\"Compactor Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Forward Plate Compactor\",\"Reversible Plate Compactor\",\"Tamping Rammer (Jumping Jack)\",\"Walk-Behind Vibratory Roller\"]},{\"id\":\"operatingWeightKg\",\"label\":\"Operating Weight (kg)\",\"type\":\"number\",\"required\":false,\"placeholder\":\"e.g. 70, 90, 160\"},{\"id\":\"engineType\",\"label\":\"Engine Type\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine (Honda GX)\",\"Diesel Engine (Hatz/Yanmar)\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000222"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false},{\"id\":\"needleDiameterMm\",\"label\":\"Needle Diameter (mm)\",\"type\":\"select\",\"required\":true,\"options\":[\"25mm\",\"35mm\",\"45mm\",\"50mm\",\"60mm\"]},{\"id\":\"shaftLengthM\",\"label\":\"Flexible Shaft Length (m)\",\"type\":\"select\",\"required\":true,\"options\":[\"4m\",\"5m\",\"6m\"]},{\"id\":\"driveUnit\",\"label\":\"Drive Unit\",\"type\":\"select\",\"required\":true,\"options\":[\"Petrol Engine Drive\",\"Electric Motor Drive (230V)\",\"Handheld Portable Electric\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000223"),
                column: "AttributeSchema",
                value: "[{\"id\":\"brand\",\"label\":\"Brand\",\"type\":\"string\",\"required\":false,\"placeholder\":\"e.g. 3M DBI-SALA, Karam, Miller\"},{\"id\":\"certificationStandard\",\"label\":\"Safety Certification\",\"type\":\"select\",\"required\":true,\"options\":[\"EN 361 (CE Certified)\",\"ANSI Z359\",\"OSHA Compliant\"]},{\"id\":\"attachmentPoints\",\"label\":\"Attachment D-Rings\",\"type\":\"select\",\"required\":false,\"options\":[\"1-Point (Dorsal)\",\"2-Point (Dorsal \\u0026 Sternal)\",\"4-Point (Dorsal, Sternal, Work Positioning)\"]}]");

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000224"),
                column: "AttributeSchema",
                value: "[{\"id\":\"standard\",\"label\":\"Safety Standard\",\"type\":\"select\",\"required\":true,\"options\":[\"EN 397 (Industrial)\",\"ANSI/ISEA Z89.1 Type 1 Class E\",\"SLS 614\"]},{\"id\":\"colour\",\"label\":\"Helmet Colour\",\"type\":\"select\",\"required\":true,\"options\":[\"White (Engineers/Managers)\",\"Yellow (General Labor)\",\"Blue (Electricians/Carpenters)\",\"Green (Safety Officers)\",\"Orange\",\"Red\"]},{\"id\":\"suspensionType\",\"label\":\"Suspension\",\"type\":\"select\",\"required\":false,\"options\":[\"Ratchet Wheel Adjustment\",\"Pin-Lock Adjustment\"]}]");
        }
    }
}
