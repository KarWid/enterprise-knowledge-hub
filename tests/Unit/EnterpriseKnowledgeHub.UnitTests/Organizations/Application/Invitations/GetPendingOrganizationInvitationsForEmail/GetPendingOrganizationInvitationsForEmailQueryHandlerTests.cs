using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;

public class GetPendingOrganizationInvitationsForEmailQueryHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyPendingNonExpiredInvitationsMatchingEmailCaseInsensitively()
    {
        using var db = CreateDbContext();
        var organizationId = Guid.NewGuid();

        var pendingInvitation = OrganizationInvitation.Create(
            organizationId, "user@example.com", "token-1", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        var acceptedInvitation = OrganizationInvitation.Create(
            organizationId, "user@example.com", "token-2", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        acceptedInvitation.Accept();

        var otherEmailInvitation = OrganizationInvitation.Create(
            organizationId, "someone-else@example.com", "token-3", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        db.OrganizationInvitations.AddRange(pendingInvitation, acceptedInvitation, otherEmailInvitation);
        await db.SaveChangesAsync();

        var handler = new GetPendingOrganizationInvitationsForEmailQueryHandler(db);
        var result = await handler.Handle(
            new GetPendingOrganizationInvitationsForEmailQuery("USER@example.com"), CancellationToken.None);

        var invitation = Assert.Single(result.Invitations);
        Assert.Equal(pendingInvitation.Id, invitation.Id);
        Assert.Equal(organizationId, invitation.OrganizationId);
    }

    [Fact]
    public async Task Handle_WhenNoInvitationsForEmail_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var handler = new GetPendingOrganizationInvitationsForEmailQueryHandler(db);

        var result = await handler.Handle(
            new GetPendingOrganizationInvitationsForEmailQuery("nobody@example.com"), CancellationToken.None);

        Assert.Empty(result.Invitations);
    }
}
