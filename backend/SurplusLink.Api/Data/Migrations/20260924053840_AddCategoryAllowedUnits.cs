using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAllowedUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "AllowedUnits",
                table: "Categories",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            // Preserve historical unit choices; do not rewrite listing/request data or matching state.
            migrationBuilder.Sql("""
                UPDATE "Categories" c SET "AllowedUnits" = ARRAY(
                    SELECT DISTINCT normalized FROM (
                        SELECT lower(regexp_replace(trim("Unit"), '\s+', ' ', 'g')) AS normalized
                        FROM "Listings" WHERE "CategoryId" = c."Id"
                        UNION
                        SELECT lower(regexp_replace(trim("Unit"), '\s+', ' ', 'g'))
                        FROM "MaterialRequests" WHERE "CategoryId" = c."Id"
                    ) units WHERE normalized <> '' ORDER BY normalized
                );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedUnits",
                table: "Categories");
        }
    }
}
