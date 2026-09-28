using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogFirstBuyerRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuyerPreferencesJson",
                table: "MaterialRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConstructionItemTemplateId",
                table: "MaterialRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_ConstructionItemTemplateId",
                table: "MaterialRequests",
                column: "ConstructionItemTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_ConstructionItemTemplates_ConstructionItemTemplateId",
                table: "MaterialRequests",
                column: "ConstructionItemTemplateId",
                principalTable: "ConstructionItemTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequests_ConstructionItemTemplates_ConstructionItemTemplateId",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_ConstructionItemTemplateId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "BuyerPreferencesJson",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "ConstructionItemTemplateId",
                table: "MaterialRequests");
        }
    }
}
