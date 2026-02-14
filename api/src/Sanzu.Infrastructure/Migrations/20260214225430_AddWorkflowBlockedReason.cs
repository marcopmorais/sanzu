using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowBlockedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockedReasonCode",
                table: "WorkflowStepInstances",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedReasonDetail",
                table: "WorkflowStepInstances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedReasonCode",
                table: "WorkflowStepInstances");

            migrationBuilder.DropColumn(
                name: "BlockedReasonDetail",
                table: "WorkflowStepInstances");
        }
    }
}
