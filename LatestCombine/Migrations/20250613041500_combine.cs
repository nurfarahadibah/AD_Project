using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class combine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComplianceCategoryId1",
                table: "JenisForms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ComplianceCategories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ComplianceCategories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "ComplianceCategories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "ComplianceCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JenisForms_ComplianceCategoryId1",
                table: "JenisForms",
                column: "ComplianceCategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId1",
                table: "JenisForms",
                column: "ComplianceCategoryId1",
                principalTable: "ComplianceCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId1",
                table: "JenisForms");

            migrationBuilder.DropIndex(
                name: "IX_JenisForms_ComplianceCategoryId1",
                table: "JenisForms");

            migrationBuilder.DropColumn(
                name: "ComplianceCategoryId1",
                table: "JenisForms");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ComplianceCategories");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ComplianceCategories");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ComplianceCategories");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "ComplianceCategories");
        }
    }
}
