using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    public partial class Story1_6_FailedPaymentsAndRecovery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedPaymentAttempts",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentFailedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPaymentFailureReason",
                table: "Organizations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentRetryAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentReminderAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentReminderSentAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedPaymentAttempts",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastPaymentFailedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastPaymentFailureReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "NextPaymentRetryAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "NextPaymentReminderAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastPaymentReminderSentAt",
                table: "Organizations");
        }
    }
}
