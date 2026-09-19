using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Domain;

public class OrganizationTests
{
    [Fact]
    public void Create_WithValidName_SetsExpectedProperties()
    {
        var organization = Organization.Create("  Acme Corp  ");

        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.Equal("Acme Corp", organization.Name);
        Assert.Equal(OrganizationStatus.Active, organization.Status);
        Assert.Empty(organization.Memberships);
    }

    [Fact]
    public void Create_WhenNameIsEmpty_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Organization.Create(""));
    }

    [Fact]
    public void AddOwner_AddsMembershipWithOrganizationOwnerRole()
    {
        var organization = Organization.Create("Acme Corp");
        var userId = Guid.NewGuid();

        organization.AddOwner(userId);

        var membership = Assert.Single(organization.Memberships);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(organization.Id, membership.OrganizationId);
        Assert.Equal(OrganizationRole.OrganizationOwner, membership.Role);
    }

    [Fact]
    public void AddMember_WhenUserAlreadyMember_DoesNotAddDuplicateOrChangeRole()
    {
        var organization = Organization.Create("Acme Corp");
        var userId = Guid.NewGuid();
        organization.AddMember(userId, OrganizationRole.Employee);

        organization.AddMember(userId, OrganizationRole.OrganizationAdmin);

        var membership = Assert.Single(organization.Memberships);
        Assert.Equal(OrganizationRole.Employee, membership.Role);
    }
}
