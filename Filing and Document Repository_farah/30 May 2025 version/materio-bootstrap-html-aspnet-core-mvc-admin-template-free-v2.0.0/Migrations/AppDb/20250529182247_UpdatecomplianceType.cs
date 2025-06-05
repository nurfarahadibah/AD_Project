using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AspnetCoreMvcFull.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class UpdatecomplianceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "ComplianceFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComplianceCategoryId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceFolders_ComplianceCategories_ComplianceCategoryId",
                        column: x => x.ComplianceCategoryId,
                        principalTable: "ComplianceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequiredDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ComplianceFolderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequiredDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequiredDocuments_ComplianceFolders_ComplianceFolderId",
                        column: x => x.ComplianceFolderId,
                        principalTable: "ComplianceFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ComplianceFolderId = table.Column<int>(type: "int", nullable: false),
                    RequiredDocumentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_ComplianceFolders_ComplianceFolderId",
                        column: x => x.ComplianceFolderId,
                        principalTable: "ComplianceFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documents_RequiredDocuments_RequiredDocumentId",
                        column: x => x.RequiredDocumentId,
                        principalTable: "RequiredDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ComplianceCategories",
                columns: new[] { "Id", "Code", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "SOX", "Regulations for financial reporting.", "SOX (Sarbanes-Oxley)" },
                    { 2, "ISO", "Information security management system standard.", "ISO 27001" },
                    { 3, "GDPR", "General Data Protection Regulation.", "GDPR" },
                    { 4, "FINAUD", "Internal or external financial review.", "Financial Audit" },
                    { 5, "SECCOMP", "Adherence to security policies and standards.", "Security Compliance" },
                    { 6, "QUALMAN", "Ensuring consistent quality of products/services.", "Quality Management" },
                    { 7, "RISKMAN", "Identifying and mitigating potential risks.", "Risk Management" },
                    { 8, "CUST", "User-defined compliance type.", "Custom" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceFolders_ComplianceCategoryId",
                table: "ComplianceFolders",
                column: "ComplianceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ComplianceFolderId",
                table: "Documents",
                column: "ComplianceFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RequiredDocumentId",
                table: "Documents",
                column: "RequiredDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredDocuments_ComplianceFolderId",
                table: "RequiredDocuments",
                column: "ComplianceFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "RequiredDocuments");

            migrationBuilder.DropTable(
                name: "ComplianceFolders");

            migrationBuilder.DropTable(
                name: "ComplianceCategories");
        }
    }
}
