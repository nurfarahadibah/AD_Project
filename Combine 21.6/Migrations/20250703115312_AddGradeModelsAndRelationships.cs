using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeModelsAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "GradeRanges",
                newName: "GradeRangeId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "GradeConfigurations",
                newName: "GradeConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GradeRangeId",
                table: "GradeRanges",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "GradeConfigurationId",
                table: "GradeConfigurations",
                newName: "Id");
        }
    }
}
