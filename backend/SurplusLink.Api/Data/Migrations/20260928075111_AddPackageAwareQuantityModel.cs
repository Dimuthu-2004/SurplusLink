using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageAwareQuantityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "Reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "Offers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseUnit",
                table: "Listings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackageSize",
                table: "Listings",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageType",
                table: "Listings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuantityMode",
                table: "Listings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<int>(
                name: "ReservedPackageCount",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings",
                sql: "(\"QuantityMode\" NOT IN ('PACKAGE','PIECE')) OR (\"PackageCount\" IS NOT NULL AND \"PackageSize\" IS NOT NULL AND \"PackageCount\" > 0 AND \"PackageSize\" > 0 AND \"ReservedPackageCount\" >= 0 AND \"ReservedPackageCount\" <= \"PackageCount\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "BaseUnit",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "QuantityMode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ReservedPackageCount",
                table: "Listings");
        }
    }
}
