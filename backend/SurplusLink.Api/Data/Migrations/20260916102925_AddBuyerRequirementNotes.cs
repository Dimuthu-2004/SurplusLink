using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerRequirementNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MaterialRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MaterialRequests");
        }
    }
}
