using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurplusLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionConfirmationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BuyerReceivedConfirmedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationDeadlineUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpNotifiedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerApprovedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionReasonCode",
                table: "Transactions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedByManagerId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerHandoverConfirmedAtUtc",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status_ConfirmationDeadlineUtc",
                table: "Transactions",
                columns: new[] { "Status", "ConfirmationDeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_TransactionId",
                table: "Reservations",
                column: "TransactionId");

            // Historic approved rows predate the server-owned deadline. Preserve
            // their lifecycle with the shipped 30-day default; new approvals use
            // TransactionConfirmation:WindowDays at runtime.
            migrationBuilder.Sql("""
                UPDATE "Transactions"
                SET "ManagerApprovedAtUtc" = "UpdatedAtUtc",
                    "ConfirmationDeadlineUtc" = "UpdatedAtUtc" + INTERVAL '30 days'
                WHERE "Status" IN ('APPROVED', 'HANDED_OVER')
                  AND "ManagerApprovedAtUtc" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Status_ConfirmationDeadlineUtc",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_TransactionId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BuyerReceivedConfirmedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ConfirmationDeadlineUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FollowUpNotifiedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ManagerApprovedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ResolutionReasonCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ResolvedByManagerId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SellerHandoverConfirmedAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "AuditLogs");
        }
    }
}
