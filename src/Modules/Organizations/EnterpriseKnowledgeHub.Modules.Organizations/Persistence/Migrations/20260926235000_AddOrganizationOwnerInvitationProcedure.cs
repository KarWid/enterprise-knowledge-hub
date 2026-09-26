using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseKnowledgeHub.Modules.Organizations.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrganizationsDbContext))]
    [Migration("20260926235000_AddOrganizationOwnerInvitationProcedure")]
    public partial class AddOrganizationOwnerInvitationProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE [organizations].[AddOrganizationOwnerInvitation]
                    @Email nvarchar(256),
                    @TokenHash nvarchar(128),
                    @ExpiresAt datetime2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF NULLIF(LTRIM(RTRIM(@Email)), N'') IS NULL
                        THROW 50000, 'Email is required.', 1;

                    IF NULLIF(LTRIM(RTRIM(@TokenHash)), N'') IS NULL
                        THROW 50001, 'Token hash is required.', 1;

                    IF @ExpiresAt <= SYSUTCDATETIME()
                        THROW 50002, 'Invitation expiration date must be in the future.', 1;

                    DECLARE @InvitationId uniqueidentifier = NEWID();

                    INSERT INTO [organizations].[OrganizationOwnerInvitations]
                        ([Id], [Email], [TokenHash], [Status], [CreatedAt], [ExpiresAt], [AcceptedAt])
                    VALUES
                        (@InvitationId, @Email, @TokenHash, N'Pending', SYSUTCDATETIME(), @ExpiresAt, NULL);

                    SELECT @InvitationId AS [InvitationId];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP PROCEDURE IF EXISTS [organizations].[AddOrganizationOwnerInvitation];");
        }
    }
}
