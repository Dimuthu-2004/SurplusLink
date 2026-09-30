using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerRequirementInputSemantics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EnteredQuantity",
                table: "MaterialRequests",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnteredUnit",
                table: "MaterialRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputMode",
                table: "MaterialRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "BASE_QUANTITY");

            migrationBuilder.AddColumn<string>(
                name: "PackageBaseUnit",
                table: "MaterialRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreferredPackageSize",
                table: "MaterialRequests",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "BuyerInputModes",
                table: "ConstructionItemTemplates",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000206"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000207"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000208"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000209"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020a"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020b"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020c"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020d"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020e"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000020f"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000210"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000211"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000212"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000213"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000214"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000215"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000216"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000217"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000218"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000219"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021a"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021b"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021c"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021d"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021e"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000021f"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000220"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000221"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000222"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000223"),
                column: "BuyerInputModes",
                value: new string[0]);

            migrationBuilder.UpdateData(
                table: "ConstructionItemTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000224"),
                column: "BuyerInputModes",
                value: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnteredQuantity",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "EnteredUnit",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "InputMode",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "PackageBaseUnit",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "PreferredPackageSize",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "BuyerInputModes",
                table: "ConstructionItemTemplates");
        }
    }
}
