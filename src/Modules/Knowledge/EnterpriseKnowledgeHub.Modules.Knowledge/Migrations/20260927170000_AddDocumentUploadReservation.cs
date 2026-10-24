using System;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
    // This hand-written migration has no Designer partial; keep its discovery metadata here.
    [DbContext(typeof(KnowledgeDbContext))]
    [Migration("20260927170000_AddDocumentUploadReservation")]
    public partial class AddDocumentUploadReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UploadExpiresAt",
                schema: "knowledge",
                table: "Documents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadExpiresAt",
                schema: "knowledge",
                table: "Documents");
        }
    }
}
