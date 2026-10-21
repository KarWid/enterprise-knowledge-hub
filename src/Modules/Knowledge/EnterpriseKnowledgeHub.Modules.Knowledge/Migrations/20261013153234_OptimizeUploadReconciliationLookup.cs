using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeUploadReconciliationLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UploadInbox_Kind_BlobReference",
                schema: "knowledge",
                table: "UploadInbox",
                columns: new[] { "Kind", "BlobReference" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadInbox_Kind_BlobReference",
                schema: "knowledge",
                table: "UploadInbox");
        }
    }
}
