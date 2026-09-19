using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Organizations.GetUserOrganizations;

public class GetUserOrganizationsQueryHandlerTests
{
    private static OrganizationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrganizationsDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsOrganizationsForCurrentUserOnly()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();

        var myOrg = Organization.Create("My Org");
        myOrg.AddMember(userId, OrganizationRole.OrganizationAdmin);

        var otherOrg = Organization.Create("Other Org");
        otherOrg.AddMember(Guid.NewGuid(), OrganizationRole.Employee);

        db.Organizations.AddRange(myOrg, otherOrg);
        await db.SaveChangesAsync();

        var handler = new GetUserOrganizationsQueryHandler(new FakeUserInfoService(new FakeUserInfo(userId)), db);
        var result = await handler.Handle(new GetUserOrganizationsQuery(), CancellationToken.None);

        var item = Assert.Single(result.Organizations);
        Assert.Equal(myOrg.Id, item.Id);
        Assert.Equal("My Org", item.Name);
        Assert.Equal(OrganizationRole.OrganizationAdmin, item.Role);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoMemberships_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var handler = new GetUserOrganizationsQueryHandler(
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())), db);

        var result = await handler.Handle(new GetUserOrganizationsQuery(), CancellationToken.None);

        Assert.Empty(result.Organizations);
    }
}
