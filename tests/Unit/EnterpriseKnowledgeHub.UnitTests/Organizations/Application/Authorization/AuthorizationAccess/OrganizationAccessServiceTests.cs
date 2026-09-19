using EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Authorization.AuthorizationAccess;

public class OrganizationAccessServiceTests
{
    [Fact]
    public async Task GetCurrentOrganizationMembershipAsync_WhenNoOrganizationOnRequest_ThrowsOrganizationAccessDeniedException()
    {
        var service = new OrganizationAccessService(
            new FakeMediator(_ => throw new InvalidOperationException("Mediator should not be called.")),
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentOrganization { OrganizationId = null });

        await Assert.ThrowsAsync<OrganizationAccessDeniedException>(
            () => service.GetCurrentOrganizationMembershipAsync(CancellationToken.None));
    }

    // Tenant isolation: a user with no active membership in the requested organization must be denied.
    [Fact]
    public async Task GetCurrentOrganizationMembershipAsync_WhenUserHasNoMembershipInOrganization_ThrowsOrganizationAccessDeniedException()
    {
        var service = new OrganizationAccessService(
            new FakeMediator(_ => new GetMembershipForUserAndOrganizationResult(false, Guid.Empty, default)),
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())),
            new FakeCurrentOrganization { OrganizationId = Guid.NewGuid() });

        await Assert.ThrowsAsync<OrganizationAccessDeniedException>(
            () => service.GetCurrentOrganizationMembershipAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentOrganizationMembershipAsync_WhenUserHasMembership_ReturnsMembership()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        var service = new OrganizationAccessService(
            new FakeMediator(_ => new GetMembershipForUserAndOrganizationResult(true, membershipId, OrganizationRole.OrganizationAdmin)),
            new FakeUserInfoService(new FakeUserInfo(userId)),
            new FakeCurrentOrganization { OrganizationId = organizationId });

        var membership = await service.GetCurrentOrganizationMembershipAsync(CancellationToken.None);

        Assert.Equal(membershipId, membership.MembershipId);
        Assert.Equal(organizationId, membership.OrganizationId);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(nameof(OrganizationRole.OrganizationAdmin), membership.Role);
    }
}
