using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_ComplianceFolders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JenisForms",
                columns: table => new
                {
                    FormTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComplianceCategoryId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ComplianceCategoryId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JenisForms", x => x.FormTypeId);
                    table.ForeignKey(
                        name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId",
                        column: x => x.ComplianceCategoryId,
                        principalTable: "ComplianceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JenisForms_ComplianceCategories_ComplianceCategoryId1",
                        column: x => x.ComplianceCategoryId1,
                        principalTable: "ComplianceCategories",
                        principalColumn: "Id");
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
                    PercentageScore = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    OriginalAuditInstanceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditInstances", x => x.AuditInstanceId);
                    table.ForeignKey(
                        name: "FK_AuditInstances_AuditInstances_OriginalAuditInstanceId",
                        column: x => x.OriginalAuditInstanceId,
                        principalTable: "AuditInstances",
                        principalColumn: "AuditInstanceId");
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
                name: "FormSections",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSections", x => x.SectionId);
                    table.ForeignKey(
                        name: "FK_FormSections_JenisForms_FormTypeId",
                        column: x => x.FormTypeId,
                        principalTable: "JenisForms",
                        principalColumn: "FormTypeId",
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

            migrationBuilder.CreateTable(
                name: "FormItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    HasLooping = table.Column<bool>(type: "bit", nullable: false),
                    LoopCount = table.Column<int>(type: "int", nullable: true),
                    LoopLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_FormItems_FormSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "FormSections",
                        principalColumn: "SectionId",
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
                    ResponseValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoredValue = table.Column<int>(type: "int", nullable: true),
                    MaxPossibleScore = table.Column<int>(type: "int", nullable: true),
                    LoopIndex = table.Column<int>(type: "int", nullable: true),
                    OriginalAuditResponseId = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "CorrectiveActions",
                columns: table => new
                {
                    CorrectiveActionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditInstanceId = table.Column<int>(type: "int", nullable: false),
                    AuditResponseId = table.Column<int>(type: "int", nullable: false),
                    CorrectiveActionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectiveActions", x => x.CorrectiveActionId);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_AuditInstances_AuditInstanceId",
                        column: x => x.AuditInstanceId,
                        principalTable: "AuditInstances",
                        principalColumn: "AuditInstanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_AuditResponses_AuditResponseId",
                        column: x => x.AuditResponseId,
                        principalTable: "AuditResponses",
                        principalColumn: "AuditResponseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditInstances_FormTypeId",
                table: "AuditInstances",
                column: "FormTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInstances_OriginalAuditInstanceId",
                table: "AuditInstances",
                column: "OriginalAuditInstanceId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceCategories_TenantId",
                table: "ComplianceCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceFolders_ComplianceCategoryId",
                table: "ComplianceFolders",
                column: "ComplianceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceFolders_TenantId",
                table: "ComplianceFolders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_AuditInstanceId",
                table: "CorrectiveActions",
                column: "AuditInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_AuditResponseId",
                table: "CorrectiveActions",
                column: "AuditResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ComplianceFolderId",
                table: "Documents",
                column: "ComplianceFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RequiredDocumentId",
                table: "Documents",
                column: "RequiredDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FormItems_SectionId",
                table: "FormItems",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_FormTypeId",
                table: "FormSections",
                column: "FormTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JenisForms_ComplianceCategoryId",
                table: "JenisForms",
                column: "ComplianceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JenisForms_ComplianceCategoryId1",
                table: "JenisForms",
                column: "ComplianceCategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredDocuments_ComplianceFolderId",
                table: "RequiredDocuments",
                column: "ComplianceFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrectiveActions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "AuditResponses");

            migrationBuilder.DropTable(
                name: "RequiredDocuments");

            migrationBuilder.DropTable(
                name: "AuditInstances");

            migrationBuilder.DropTable(
                name: "FormItems");

            migrationBuilder.DropTable(
                name: "ComplianceFolders");

            migrationBuilder.DropTable(
                name: "FormSections");

            migrationBuilder.DropTable(
                name: "JenisForms");

            migrationBuilder.DropTable(
                name: "ComplianceCategories");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
