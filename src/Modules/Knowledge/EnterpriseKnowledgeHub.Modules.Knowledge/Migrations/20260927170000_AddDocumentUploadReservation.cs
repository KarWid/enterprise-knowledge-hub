using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Migrations
{
    /// <inheritdoc />
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
