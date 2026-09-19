using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.HasPendingInvitationForEmail;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Invitations.HasPendingInvitationForEmail;

public class HasPendingInvitationForEmailQueryHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenPendingOwnerInvitationExists_ReturnsTrue()
    {
        using var db = CreateDbContext();
        db.OrganizationOwnerInvitations.Add(
            OrganizationOwnerInvitation.Create("owner@example.com", "token", DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();

        var handler = new HasPendingInvitationForEmailQueryHandler(db);
        var result = await handler.Handle(new HasPendingInvitationForEmailQuery("owner@example.com"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_WhenPendingOrganizationInvitationExists_ReturnsTrue()
    {
        using var db = CreateDbContext();
        db.OrganizationInvitations.Add(
            OrganizationInvitation.Create(Guid.NewGuid(), "member@example.com", "token", Guid.NewGuid(), DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();

        var handler = new HasPendingInvitationForEmailQueryHandler(db);
        var result = await handler.Handle(new HasPendingInvitationForEmailQuery("member@example.com"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_WhenNoPendingInvitationsForEmail_ReturnsFalse()
    {
        using var db = CreateDbContext();
        var handler = new HasPendingInvitationForEmailQueryHandler(db);

        var result = await handler.Handle(new HasPendingInvitationForEmailQuery("nobody@example.com"), CancellationToken.None);

        Assert.False(result);
    }
}
