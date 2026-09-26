using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;

internal sealed class OrganizationAccessService(
    IMediator _mediator,
    IUserInfoService _userInfoService,
    ICurrentOrganization _currentOrganization) : IOrganizationAccessService
{
    public async Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken)
    {
        var organizationId = _currentOrganization.OrganizationId
            ?? throw new OrganizationAccessDeniedException("No organization was specified for this request.");

        var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);

        var result = await _mediator.Send(
            new GetMembershipForUserAndOrganizationQuery(userInfo.UserId, organizationId),
            cancellationToken);

        if (!result.Found)
            throw new OrganizationAccessDeniedException("You do not have access to this organization.");

        return new OrganizationMembership(result.MembershipId, organizationId, userInfo.UserId, result.Role.ToString());
    }

    // No caching at this layer — see CachedOrganizationAccessService for the cached decorator.
    public void ClearCache(Guid userId, Guid organizationId)
    {
    }
}
