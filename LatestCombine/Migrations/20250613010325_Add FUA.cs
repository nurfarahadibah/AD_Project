﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class AddFUA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubmittedValue",
                table: "AuditResponses",
                newName: "ResponseValue");

            migrationBuilder.AlterColumn<int>(
                name: "ScoredValue",
                table: "AuditResponses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "OriginalAuditResponseId",
                table: "AuditResponses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalAuditResponseId",
                table: "AuditResponses");

            migrationBuilder.RenameColumn(
                name: "ResponseValue",
                table: "AuditResponses",
                newName: "SubmittedValue");

            migrationBuilder.AlterColumn<int>(
                name: "ScoredValue",
                table: "AuditResponses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
