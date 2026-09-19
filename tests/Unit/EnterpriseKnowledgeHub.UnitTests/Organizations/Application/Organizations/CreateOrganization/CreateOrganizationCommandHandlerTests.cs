using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.CreateOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Organizations.CreateOrganization;

public class CreateOrganizationCommandHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenUserIsAlreadyAnOwner_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var existingOrg = Organization.Create("Existing Org");
        existingOrg.AddOwner(userId);
        db.Organizations.Add(existingOrg);
        await db.SaveChangesAsync();

        var handler = new CreateOrganizationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(userId)),
            new FakeCurrentUser { Email = "owner@example.com" });

        await Assert.ThrowsAsync<OrganizationsDomainException>(
            () => handler.Handle(new CreateOrganizationCommand("New Org"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNoPendingOwnerInvitationForEmail_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext();

        var handler = new CreateOrganizationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentUser { Email = "not-invited@example.com" });

        await Assert.ThrowsAsync<OrganizationsDomainException>(
            () => handler.Handle(new CreateOrganizationCommand("New Org"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesOrganizationAddsOwnerAndAcceptsInvitation()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var ownerInvitation = OrganizationOwnerInvitation.Create("owner@example.com", "token-hash", DateTime.UtcNow.AddDays(7));
        db.OrganizationOwnerInvitations.Add(ownerInvitation);
        await db.SaveChangesAsync();

        var handler = new CreateOrganizationCommandHandler(
            db,
            new FakeUserInfoService(new FakeUserInfo(userId)),
            new FakeCurrentUser { Email = "OWNER@example.com" });

        var result = await handler.Handle(new CreateOrganizationCommand("New Org"), CancellationToken.None);

        Assert.Equal("New Org", result.Name);

        var persistedOrg = await db.Organizations.Include(o => o.Memberships).SingleAsync();
        Assert.Equal(result.Id, persistedOrg.Id);
        var membership = Assert.Single(persistedOrg.Memberships);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(OrganizationRole.OrganizationOwner, membership.Role);

        var persistedInvitation = await db.OrganizationOwnerInvitations.SingleAsync();
        Assert.Equal(InvitationStatus.Accepted, persistedInvitation.Status);
    }
}
