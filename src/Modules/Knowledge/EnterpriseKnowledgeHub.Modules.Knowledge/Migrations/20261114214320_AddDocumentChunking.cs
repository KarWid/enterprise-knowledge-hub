using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_DocumentTextExtractions_OrganizationId_DocumentId_Id",
                schema: "knowledge",
                table: "DocumentTextExtractions",
                columns: new[] { "OrganizationId", "DocumentId", "Id" });

            migrationBuilder.CreateTable(
                name: "DocumentChunkings",
                schema: "knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceETag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FormatId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChunkerVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConfigurationKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetTokens = table.Column<int>(type: "int", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    OverlapTokens = table.Column<int>(type: "int", nullable: false),
                    MaxChunksPerDocument = table.Column<int>(type: "int", nullable: false),
                    MaxSourceCharacters = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChunkCount = table.Column<int>(type: "int", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunkings", x => x.Id);
                    table.UniqueConstraint("AK_DocumentChunkings_OrganizationId_DocumentId_Id", x => new { x.OrganizationId, x.DocumentId, x.Id });
                    table.ForeignKey(
                        name: "FK_DocumentChunkings_DocumentTextExtractions_OrganizationId_DocumentId_ExtractionId",
                        columns: x => new { x.OrganizationId, x.DocumentId, x.ExtractionId },
                        principalSchema: "knowledge",
                        principalTable: "DocumentTextExtractions",
                        principalColumns: new[] { "OrganizationId", "DocumentId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                schema: "knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadingPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TokenCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_DocumentChunkings_OrganizationId_DocumentId_ChunkingId",
                        columns: x => new { x.OrganizationId, x.DocumentId, x.ChunkingId },
                        principalSchema: "knowledge",
                        principalTable: "DocumentChunkings",
                        principalColumns: new[] { "OrganizationId", "DocumentId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunkings_ExtractionId_ConfigurationKey",
                schema: "knowledge",
                table: "DocumentChunkings",
                columns: new[] { "ExtractionId", "ConfigurationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunkings_OrganizationId_DocumentId_ExtractionId",
                schema: "knowledge",
                table: "DocumentChunkings",
                columns: new[] { "OrganizationId", "DocumentId", "ExtractionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunkings_Status_NextAttemptAt_LeaseExpiresAt",
                schema: "knowledge",
                table: "DocumentChunkings",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_ChunkingId_Ordinal",
                schema: "knowledge",
                table: "DocumentChunks",
                columns: new[] { "ChunkingId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_OrganizationId_DocumentId_ChunkingId",
                schema: "knowledge",
                table: "DocumentChunks",
                columns: new[] { "OrganizationId", "DocumentId", "ChunkingId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentChunks",
                schema: "knowledge");

            migrationBuilder.DropTable(
                name: "DocumentChunkings",
                schema: "knowledge");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DocumentTextExtractions_OrganizationId_DocumentId_Id",
                schema: "knowledge",
                table: "DocumentTextExtractions");
        }
    }
}
