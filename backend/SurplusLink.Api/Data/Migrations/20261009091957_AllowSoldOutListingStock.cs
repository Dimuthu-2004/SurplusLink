using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowSoldOutListingStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Quantity_Positive",
                table: "Listings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings",
                sql: "(\"QuantityMode\" NOT IN ('PACKAGE','PIECE')) OR (\"PackageCount\" IS NOT NULL AND \"PackageSize\" IS NOT NULL AND \"PackageSize\" > 0 AND \"ReservedPackageCount\" >= 0 AND \"ReservedPackageCount\" <= \"PackageCount\" AND ((\"Status\" = 'SOLD' AND \"PackageCount\" = 0 AND \"ReservedPackageCount\" = 0) OR (\"Status\" <> 'SOLD' AND \"PackageCount\" > 0)))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Quantity_Positive",
                table: "Listings",
                sql: "(\"Status\" = 'SOLD' AND \"Quantity\" = 0 AND \"ReservedQuantity\" = 0) OR (\"Status\" <> 'SOLD' AND \"Quantity\" > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Quantity_Positive",
                table: "Listings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Package_Stock",
                table: "Listings",
                sql: "(\"QuantityMode\" NOT IN ('PACKAGE','PIECE')) OR (\"PackageCount\" IS NOT NULL AND \"PackageSize\" IS NOT NULL AND \"PackageCount\" > 0 AND \"PackageSize\" > 0 AND \"ReservedPackageCount\" >= 0 AND \"ReservedPackageCount\" <= \"PackageCount\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Quantity_Positive",
                table: "Listings",
                sql: "\"Quantity\" > 0");
        }
    }
}
