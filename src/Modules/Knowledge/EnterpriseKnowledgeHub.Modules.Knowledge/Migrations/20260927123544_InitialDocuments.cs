using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    public partial class InitialDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "knowledge");

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BlobReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_BlobReference",
                schema: "knowledge",
                table: "Documents",
                column: "BlobReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OrganizationId_CreatedAt",
                schema: "knowledge",
                table: "Documents",
                columns: new[] { "OrganizationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents",
                schema: "knowledge");
        }
    }
}
