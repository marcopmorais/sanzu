using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story13_1_TenantHealthScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles");

            migrationBuilder.CreateTable(
                name: "RemediationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QueueItemId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuditNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImpactSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VerificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerificationResult = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CommittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemediationActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantHealthScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    BillingScore = table.Column<int>(type: "int", nullable: false),
                    CaseCompletionScore = table.Column<int>(type: "int", nullable: false),
                    OnboardingScore = table.Column<int>(type: "int", nullable: false),
                    HealthBand = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PrimaryIssue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHealthScores", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles",
                sql: "[RoleType] IN ('AgencyAdmin','SanzuAdmin','SanzuOps','SanzuFinance','SanzuSupport','SanzuViewer')");

            migrationBuilder.CreateIndex(
                name: "IX_RemediationActions_QueueId_QueueItemId",
                table: "RemediationActions",
                columns: new[] { "QueueId", "QueueItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RemediationActions_TenantId",
                table: "RemediationActions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthScore_TenantId_ComputedAt",
                table: "TenantHealthScores",
                columns: new[] { "TenantId", "ComputedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemediationActions");

            migrationBuilder.DropTable(
                name: "TenantHealthScores");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles",
                sql: "[RoleType] IN ('AgencyAdmin','SanzuAdmin')");
        }
    }
}
