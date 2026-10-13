using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableDocumentUploadInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                schema: "knowledge",
                table: "Documents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                schema: "knowledge",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ValidatedETag",
                schema: "knowledge",
                table: "Documents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UploadInbox",
                schema: "knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BlobReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ETag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_UploadInbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status_UploadExpiresAt",
                schema: "knowledge",
                table: "Documents",
                columns: new[] { "Status", "UploadExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadInbox_Source_EventId",
                schema: "knowledge",
                table: "UploadInbox",
                columns: new[] { "Source", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadInbox_Status_NextAttemptAt_LeaseExpiresAt",
                schema: "knowledge",
                table: "UploadInbox",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadInbox",
                schema: "knowledge");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Status_UploadExpiresAt",
                schema: "knowledge",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                schema: "knowledge",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "knowledge",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ValidatedETag",
                schema: "knowledge",
                table: "Documents");
        }
    }
}
