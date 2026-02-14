using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    public partial class Story1_4_SubscriptionChangesAndCancellation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionCancelledAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionCancellationReason",
                table: "Organizations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSubscriptionPlan",
                table: "Organizations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionCancelledAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionCancellationReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PreviousSubscriptionPlan",
                table: "Organizations");
        }
    }
}
