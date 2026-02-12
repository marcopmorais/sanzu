using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    public partial class Story1_2_AddTenantOnboardingAndInvitations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocale",
                table: "Organizations",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTimeZone",
                table: "Organizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Organizations",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultWorkflowKey",
                table: "Organizations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTemplateKey",
                table: "Organizations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_InvitedBy",
                table: "TenantInvitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Status",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email_Pending",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email", "Status" },
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultLocale",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultTimeZone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultWorkflowKey",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultTemplateKey",
                table: "Organizations");
        }
    }
}
