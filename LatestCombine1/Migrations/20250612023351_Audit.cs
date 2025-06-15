using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditInstances",
                columns: table => new
                {
                    AuditInstanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormTypeId = table.Column<int>(type: "int", nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    TotalMaxScore = table.Column<int>(type: "int", nullable: false),
                    PercentageScore = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditInstances", x => x.AuditInstanceId);
                    table.ForeignKey(
                        name: "FK_AuditInstances_JenisForms_FormTypeId",
                        column: x => x.FormTypeId,
                        principalTable: "JenisForms",
                        principalColumn: "FormTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditInstances_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditResponses",
                columns: table => new
                {
                    AuditResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditInstanceId = table.Column<int>(type: "int", nullable: false),
                    FormItemId = table.Column<int>(type: "int", nullable: false),
                    FormItemQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoredValue = table.Column<int>(type: "int", nullable: false),
                    MaxPossibleScore = table.Column<int>(type: "int", nullable: true),
                    LoopIndex = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditResponses", x => x.AuditResponseId);
                    table.ForeignKey(
                        name: "FK_AuditResponses_AuditInstances_AuditInstanceId",
                        column: x => x.AuditInstanceId,
                        principalTable: "AuditInstances",
                        principalColumn: "AuditInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditResponses_FormItems_FormItemId",
                        column: x => x.FormItemId,
                        principalTable: "FormItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditInstances_FormTypeId",
                table: "AuditInstances",
                column: "FormTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInstances_TenantId",
                table: "AuditInstances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditResponses_AuditInstanceId",
                table: "AuditResponses",
                column: "AuditInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditResponses_FormItemId",
                table: "AuditResponses",
                column: "FormItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditResponses");

            migrationBuilder.DropTable(
                name: "AuditInstances");
        }
    }
}
