using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class UpdateDocumentRequiredDocumentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequiredDocuments_Documents_DocumentId",
                table: "RequiredDocuments");

            migrationBuilder.DropIndex(
                name: "IX_RequiredDocuments_DocumentId",
                table: "RequiredDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "RequiredDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "SubmittedBy",
                table: "RequiredDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "RequiredDocumentId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RequiredDocumentId",
                table: "Documents",
                column: "RequiredDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_RequiredDocuments_RequiredDocumentId",
                table: "Documents",
                column: "RequiredDocumentId",
                principalTable: "RequiredDocuments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_RequiredDocuments_RequiredDocumentId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_RequiredDocumentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RequiredDocumentId",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "SubmittedBy",
                table: "RequiredDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "RequiredDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequiredDocuments_DocumentId",
                table: "RequiredDocuments",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredDocuments_Documents_DocumentId",
                table: "RequiredDocuments",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }
    }
}
