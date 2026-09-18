using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => new { x.UserId, x.Role });
                    table.CheckConstraint("CK_UserRoleAssignments_Role", "\"Role\" IN ('SELLER', 'BUYER', 'MANAGER')");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy first, then remove the old column in the same migration transaction.
            migrationBuilder.Sql("""
                INSERT INTO "UserRoleAssignments" ("UserId", "Role")
                SELECT "Id", "Role" FROM "Users";
                """);
            migrationBuilder.DropColumn(name: "Role", table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A single-role schema cannot represent a dual-role account. Refuse a lossy rollback.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT u."Id" FROM "Users" u
                        LEFT JOIN "UserRoleAssignments" r ON r."UserId" = u."Id"
                        GROUP BY u."Id" HAVING COUNT(r."Role") <> 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot roll back: every user must have exactly one role assignment.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("""
                UPDATE "Users" u SET "Role" = r."Role"
                FROM "UserRoleAssignments" r WHERE r."UserId" = u."Id";
                """);
            migrationBuilder.DropTable(name: "UserRoleAssignments");
        }
    }
}
