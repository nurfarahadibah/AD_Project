using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class FUA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalAuditInstanceId",
                table: "AuditInstances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditInstances_OriginalAuditInstanceId",
                table: "AuditInstances",
                column: "OriginalAuditInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditInstances_AuditInstances_OriginalAuditInstanceId",
                table: "AuditInstances",
                column: "OriginalAuditInstanceId",
                principalTable: "AuditInstances",
                principalColumn: "AuditInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditInstances_AuditInstances_OriginalAuditInstanceId",
                table: "AuditInstances");

            migrationBuilder.DropIndex(
                name: "IX_AuditInstances_OriginalAuditInstanceId",
                table: "AuditInstances");

            migrationBuilder.DropColumn(
                name: "OriginalAuditInstanceId",
                table: "AuditInstances");
        }
    }
}
