using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Domain;

public class MembershipTests
{
    [Fact]
    public void Create_SetsExpectedPropertiesAndActiveStatus()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var membership = Membership.Create(userId, organizationId, OrganizationRole.KnowledgeManager);

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(organizationId, membership.OrganizationId);
        Assert.Equal(OrganizationRole.KnowledgeManager, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }
}
