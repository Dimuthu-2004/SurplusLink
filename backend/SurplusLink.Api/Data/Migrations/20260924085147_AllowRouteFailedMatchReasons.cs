using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowRouteFailedMatchReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches");

            // Older route failures were persisted before a reason was required.
            // Normalize them before enforcing the strengthened invariant.
            migrationBuilder.Sql("""
                UPDATE "Matches"
                SET "RejectionReason" = 'ROUTE_UNAVAILABLE'
                WHERE "Status" = 'ROUTE_FAILED'
                  AND ("RejectionReason" IS NULL OR length(btrim("RejectionReason")) = 0);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches",
                sql: "(\"Status\" IN ('REJECTED', 'ROUTE_FAILED') AND length(btrim(\"RejectionReason\")) > 0 AND \"RejectionReason\" IS NOT NULL) OR (\"Status\" NOT IN ('REJECTED', 'ROUTE_FAILED') AND \"RejectionReason\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches",
                sql: "(\"Status\" = 'REJECTED' AND length(btrim(\"RejectionReason\")) > 0 AND \"RejectionReason\" IS NOT NULL) OR (\"Status\" <> 'REJECTED' AND \"RejectionReason\" IS NULL)");
        }
    }
}
