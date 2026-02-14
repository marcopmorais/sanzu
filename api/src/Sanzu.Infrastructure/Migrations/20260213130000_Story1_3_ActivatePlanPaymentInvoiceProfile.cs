using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    public partial class Story1_3_ActivatePlanPaymentInvoiceProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceProfileBillingEmail",
                table: "Organizations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceProfileCountryCode",
                table: "Organizations",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceProfileLegalName",
                table: "Organizations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceProfileVatNumber",
                table: "Organizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodReference",
                table: "Organizations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodType",
                table: "Organizations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionActivatedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionBillingCycle",
                table: "Organizations",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Organizations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceProfileBillingEmail",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "InvoiceProfileCountryCode",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "InvoiceProfileLegalName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "InvoiceProfileVatNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PaymentMethodReference",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PaymentMethodType",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionActivatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionBillingCycle",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Organizations");
        }
    }
}
