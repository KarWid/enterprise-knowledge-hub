using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptOrganizationInvitation;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Invitations.AcceptOrganizationInvitation;

public class AcceptOrganizationInvitationCommandHandlerTests
{
    private static OrganizationsDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenInvitationValidAndEmailMatches_AddsMembershipAndAcceptsInvitation()
    {
        var databaseName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        using (var seedDb = CreateDbContext(databaseName))
        {
            var organization = Organization.Create("Acme Corp");
            seedDb.Organizations.Add(organization);
            var invitation = OrganizationInvitation.Create(
                organization.Id, "user@example.com", "token-hash", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
            seedDb.OrganizationInvitations.Add(invitation);
            await seedDb.SaveChangesAsync();

            organizationId = organization.Id;
            invitationId = invitation.Id;
        }

        var userId = Guid.NewGuid();

        // Use a fresh DbContext (same in-memory store) so the handler queries load untracked entities,
        // matching how a real request-scoped DbContext behaves.
        using (var db = CreateDbContext(databaseName))
        {
            var handler = new AcceptOrganizationInvitationCommandHandler(
                db,
                new FakeUserInfoService(new FakeUserInfo(userId)),
                new FakeCurrentUser { Email = "User@Example.com" });

            var result = await handler.Handle(new AcceptOrganizationInvitationCommand(invitationId), CancellationToken.None);

            Assert.Equal(organizationId, result.OrganizationId);
            Assert.Equal("Employee", result.Role);
        }

        using var assertDb = CreateDbContext(databaseName);
        var persistedOrg = await assertDb.Organizations.Include(o => o.Memberships).SingleAsync();
        var membership = Assert.Single(persistedOrg.Memberships);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(OrganizationRole.Employee, membership.Role);

        var persistedInvitation = await assertDb.OrganizationInvitations.SingleAsync();
        Assert.Equal(InvitationStatus.Accepted, persistedInvitation.Status);
    }

    // Security-critical: must not reveal or accept an invitation addressed to a different email.
    [Fact]
    public async Task Handle_WhenInvitationEmailDoesNotMatchCurrentUser_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext(Guid.NewGuid().ToString());
        var organization = Organization.Create("Acme Corp");
        db.Organizations.Add(organization);
        var invitation = OrganizationInvitation.Create(
            organization.Id, "invited@example.com", "token-hash", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        db.OrganizationInvitations.Add(invitation);
        await db.SaveChangesAsync();

        var handler = new AcceptOrganizationInvitationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentUser { Email = "someone-else@example.com" });

        await Assert.ThrowsAsync<OrganizationsDomainException>(
            () => handler.Handle(new AcceptOrganizationInvitationCommand(invitation.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext(Guid.NewGuid().ToString());
        var handler = new AcceptOrganizationInvitationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentUser { Email = "user@example.com" });

        await Assert.ThrowsAsync<OrganizationsDomainException>(
            () => handler.Handle(new AcceptOrganizationInvitationCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenInvitationAlreadyAccepted_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext(Guid.NewGuid().ToString());
        var organization = Organization.Create("Acme Corp");
        db.Organizations.Add(organization);
        var invitation = OrganizationInvitation.Create(
            organization.Id, "user@example.com", "token-hash", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        invitation.Accept();
        db.OrganizationInvitations.Add(invitation);
        await db.SaveChangesAsync();

        var handler = new AcceptOrganizationInvitationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentUser { Email = "user@example.com" });

        await Assert.ThrowsAsync<OrganizationsDomainException>(
            () => handler.Handle(new AcceptOrganizationInvitationCommand(invitation.Id), CancellationToken.None));
    }
}
