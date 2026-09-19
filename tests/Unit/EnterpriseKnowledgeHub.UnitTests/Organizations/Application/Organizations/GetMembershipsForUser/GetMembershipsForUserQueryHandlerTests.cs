using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipsForUser;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Organizations.GetMembershipsForUser;

public class GetMembershipsForUserQueryHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveMembershipOrganizationIdsForUser()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();

        var orgWithActiveMembership = Organization.Create("Org A");
        orgWithActiveMembership.AddMember(userId, OrganizationRole.Employee);

        var orgForOtherUser = Organization.Create("Org B");
        orgForOtherUser.AddMember(Guid.NewGuid(), OrganizationRole.Employee);

        db.Organizations.AddRange(orgWithActiveMembership, orgForOtherUser);
        await db.SaveChangesAsync();

        var handler = new GetMembershipsForUserQueryHandler(db);
        var result = await handler.Handle(new GetMembershipsForUserQuery(userId), CancellationToken.None);

        var organizationId = Assert.Single(result.OrganizationIds!);
        Assert.Equal(orgWithActiveMembership.Id, organizationId);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoMemberships_ReturnsEmptyCollection()
    {
        using var db = CreateDbContext();
        var handler = new GetMembershipsForUserQueryHandler(db);

        var result = await handler.Handle(new GetMembershipsForUserQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result.OrganizationIds!);
    }
}
