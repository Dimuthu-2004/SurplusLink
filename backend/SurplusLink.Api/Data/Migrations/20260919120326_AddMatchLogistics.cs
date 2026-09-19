using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchLogistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Matches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedTransportCost",
                table: "Matches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Matches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Matches",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "GENERATED");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Matches",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Distance",
                table: "Matches",
                sql: "\"Distance\" IS NULL OR \"Distance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches",
                sql: "(\"Status\" = 'REJECTED' AND length(btrim(\"RejectionReason\")) > 0 AND \"RejectionReason\" IS NOT NULL) OR (\"Status\" <> 'REJECTED' AND \"RejectionReason\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches",
                sql: "\"Status\" IN ('GENERATED', 'RANKED', 'ROUTED', 'ROUTE_FAILED', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_TransportCost",
                table: "Matches",
                sql: "\"EstimatedTransportCost\" IS NULL OR \"EstimatedTransportCost\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Distance",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_RejectionReason",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_TransportCost",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "EstimatedTransportCost",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Matches");
        }
    }
}
