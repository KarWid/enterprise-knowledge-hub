using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;

public class GetMembershipForUserAndOrganizationQueryHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenActiveMembershipExists_ReturnsFoundWithMembershipDetails()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var userId = Guid.NewGuid();
        organization.AddMember(userId, OrganizationRole.KnowledgeManager);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var handler = new GetMembershipForUserAndOrganizationQueryHandler(db);
        var result = await handler.Handle(
            new GetMembershipForUserAndOrganizationQuery(userId, organization.Id), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal(OrganizationRole.KnowledgeManager, result.Role);
        Assert.NotEqual(Guid.Empty, result.MembershipId);
    }

    [Fact]
    public async Task Handle_WhenNoMembershipExists_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var handler = new GetMembershipForUserAndOrganizationQueryHandler(db);

        var result = await handler.Handle(
            new GetMembershipForUserAndOrganizationQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Equal(Guid.Empty, result.MembershipId);
    }

    // Tenant isolation: a membership in one organization must never be reported as "Found" for another.
    [Fact]
    public async Task Handle_WhenMembershipBelongsToDifferentOrganization_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var userId = Guid.NewGuid();
        organization.AddMember(userId, OrganizationRole.Employee);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var handler = new GetMembershipForUserAndOrganizationQueryHandler(db);

        var result = await handler.Handle(
            new GetMembershipForUserAndOrganizationQuery(userId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_WhenMembershipIsRevoked_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var organization = Organization.Create("Acme Corp");
        var userId = Guid.NewGuid();
        organization.AddMember(userId, OrganizationRole.Employee);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var membership = await db.Memberships.SingleAsync();
        db.Entry(membership).Property("Status").CurrentValue = MembershipStatus.Revoked;
        await db.SaveChangesAsync();

        var handler = new GetMembershipForUserAndOrganizationQueryHandler(db);
        var result = await handler.Handle(
            new GetMembershipForUserAndOrganizationQuery(userId, organization.Id), CancellationToken.None);

        Assert.False(result.Found);
    }
}
