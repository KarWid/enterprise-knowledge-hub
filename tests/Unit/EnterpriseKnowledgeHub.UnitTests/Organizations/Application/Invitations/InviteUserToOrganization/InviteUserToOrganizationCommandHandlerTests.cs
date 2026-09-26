using EnterpriseKnowledgeHub.Modules.Organizations.Application.Events.OrganizationInvitationCreated;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.InviteUserToOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Invitations.InviteUserToOrganization;

public class InviteUserToOrganizationCommandHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var callerId = Guid.NewGuid();
        organization.AddMember(callerId, OrganizationRole.Employee);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var handler = new InviteUserToOrganizationCommandHandler(
            db, new FakeUserInfoService(new FakeUserInfo(callerId)), new FakePublisher());

        await Assert.ThrowsAsync<OrganizationsDomainException>(() => handler.Handle(
            new InviteUserToOrganizationCommand(organization.Id, "new@example.com"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenPendingInvitationAlreadyExistsForEmail_ThrowsOrganizationsDomainException()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var ownerId = Guid.NewGuid();
        organization.AddOwner(ownerId);
        db.Organizations.Add(organization);
        db.OrganizationInvitations.Add(OrganizationInvitation.Create(
            organization.Id, "new@example.com", "token", ownerId, DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();

        var handler = new InviteUserToOrganizationCommandHandler(
            db, new FakeUserInfoService(new FakeUserInfo(ownerId)), new FakePublisher());

        await Assert.ThrowsAsync<OrganizationsDomainException>(() => handler.Handle(
            new InviteUserToOrganizationCommand(organization.Id, "new@example.com"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesInvitationAndPublishesEvent()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var ownerId = Guid.NewGuid();
        organization.AddOwner(ownerId);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var publisher = new FakePublisher();
        var handler = new InviteUserToOrganizationCommandHandler(
            db, new FakeUserInfoService(new FakeUserInfo(ownerId)), publisher);

        var result = await handler.Handle(
            new InviteUserToOrganizationCommand(organization.Id, "New@Example.com"), CancellationToken.None);

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(organization.Id, result.OrganizationId);

        var persisted = await db.OrganizationInvitations.SingleAsync();
        Assert.Equal(InvitationStatus.Pending, persisted.Status);

        var published = Assert.IsType<OrganizationInvitationCreatedEvent>(Assert.Single(publisher.PublishedNotifications));
        Assert.Equal(persisted.Id, published.InvitationId);
        Assert.Equal("new@example.com", published.RecipientEmail);
    }
}
