using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story12_1_ExtendPlatformRolesAndAdminPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_RoleType",
                table: "UserRoles",
                sql: "[RoleType] IN ('AgencyAdmin','SanzuAdmin','SanzuOps','SanzuFinance','SanzuSupport','SanzuViewer')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
