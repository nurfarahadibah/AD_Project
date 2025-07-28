using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradeConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeConfigurations_JenisForms_FormTypeId",
                        column: x => x.FormTypeId,
                        principalTable: "JenisForms",
                        principalColumn: "FormTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeConfigurationId = table.Column<int>(type: "int", nullable: false),
                    MinPercentage = table.Column<int>(type: "int", nullable: false),
                    MaxPercentage = table.Column<int>(type: "int", nullable: false),
                    GradeLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeRanges_GradeConfigurations_GradeConfigurationId",
                        column: x => x.GradeConfigurationId,
                        principalTable: "GradeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeConfigurations_FormTypeId",
                table: "GradeConfigurations",
                column: "FormTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRanges_GradeConfigurationId",
                table: "GradeRanges",
                column: "GradeConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeRanges");

            migrationBuilder.DropTable(
                name: "GradeConfigurations");
        }
    }
}
