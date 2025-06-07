using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceCategoryToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "JenisForms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<int>(
                name: "ComplianceCategoryId",
                table: "JenisForms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComplianceCategoryId",
                table: "ComplianceFolders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComplianceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JenisForms_ComplianceCategoryId",
                table: "JenisForms",
                column: "ComplianceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceFolders_ComplianceCategoryId",
                table: "ComplianceFolders",
                column: "ComplianceCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplianceFolders_ComplianceCategories_ComplianceCategoryId",
                table: "ComplianceFolders",
                column: "ComplianceCategoryId",
                principalTable: "ComplianceCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId",
                table: "JenisForms",
                column: "ComplianceCategoryId",
                principalTable: "ComplianceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplianceFolders_ComplianceCategories_ComplianceCategoryId",
                table: "ComplianceFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId",
                table: "JenisForms");

            migrationBuilder.DropTable(
                name: "ComplianceCategories");

            migrationBuilder.DropIndex(
                name: "IX_JenisForms_ComplianceCategoryId",
                table: "JenisForms");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceFolders_ComplianceCategoryId",
                table: "ComplianceFolders");

            migrationBuilder.DropColumn(
                name: "ComplianceCategoryId",
                table: "JenisForms");

            migrationBuilder.DropColumn(
                name: "ComplianceCategoryId",
                table: "ComplianceFolders");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "JenisForms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
