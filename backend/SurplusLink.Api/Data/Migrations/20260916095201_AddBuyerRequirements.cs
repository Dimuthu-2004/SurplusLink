using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_BuyerId",
                table: "MaterialRequests");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "MaterialRequests",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "MaterialRequests",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "MaterialRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "unit");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "MaterialRequests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_BuyerId_CreatedAtUtc",
                table: "MaterialRequests",
                columns: new[] { "BuyerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_Status_DeadlineUtc",
                table: "MaterialRequests",
                columns: new[] { "Status", "DeadlineUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaterialRequests_Coordinates_Valid",
                table: "MaterialRequests",
                sql: "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL AND \"Latitude\" BETWEEN -90 AND 90 AND \"Longitude\" BETWEEN -180 AND 180)");

            // Preserve existing request IDs and all matching/reservation foreign keys.
            migrationBuilder.Sql("""
                UPDATE "MaterialRequests" SET "Status" = CASE "Status"
                    WHEN 'MATCHED' THEN 'MATCH_FOUND'
                    WHEN 'FULFILLED' THEN 'COMPLETED'
                    WHEN 'EXPIRED' THEN 'CANCELLED'
                    ELSE "Status" END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaterialRequests_Status_Valid",
                table: "MaterialRequests",
                sql: "\"Status\" IN ('DRAFT', 'OPEN', 'MATCHING', 'MATCH_FOUND', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaterialRequests_Unit_NotBlank",
                table: "MaterialRequests",
                sql: "length(btrim(\"Unit\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_BuyerId_CreatedAtUtc",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_Status_DeadlineUtc",
                table: "MaterialRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaterialRequests_Coordinates_Valid",
                table: "MaterialRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaterialRequests_Status_Valid",
                table: "MaterialRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaterialRequests_Unit_NotBlank",
                table: "MaterialRequests");

            // Older code cannot read new lifecycle values. A rollback loses their finer distinctions.
            migrationBuilder.Sql("""
                UPDATE "MaterialRequests" SET "Status" = CASE "Status"
                    WHEN 'MATCH_FOUND' THEN 'MATCHED'
                    WHEN 'COMPLETED' THEN 'FULFILLED'
                    WHEN 'CANCELLED' THEN 'CANCELLED'
                    ELSE 'OPEN' END;
                """);

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "MaterialRequests");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_BuyerId",
                table: "MaterialRequests",
                column: "BuyerId");
        }
    }
}
