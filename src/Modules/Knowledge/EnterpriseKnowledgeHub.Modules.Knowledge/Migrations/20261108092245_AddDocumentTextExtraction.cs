using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTextExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Documents_OrganizationId_Id",
                schema: "knowledge",
                table: "Documents",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateTable(
                name: "DocumentTextExtractions",
                schema: "knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceETag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FormatId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExtractorVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CharacterCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTextExtractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTextExtractions_Documents_OrganizationId_DocumentId",
                        columns: x => new { x.OrganizationId, x.DocumentId },
                        principalSchema: "knowledge",
                        principalTable: "Documents",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTextExtractions_OrganizationId_DocumentId_SourceETag",
                schema: "knowledge",
                table: "DocumentTextExtractions",
                columns: new[] { "OrganizationId", "DocumentId", "SourceETag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTextExtractions_Status_NextAttemptAt_LeaseExpiresAt",
                schema: "knowledge",
                table: "DocumentTextExtractions",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTextExtractions",
                schema: "knowledge");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Documents_OrganizationId_Id",
                schema: "knowledge",
                table: "Documents");
        }
    }
}
