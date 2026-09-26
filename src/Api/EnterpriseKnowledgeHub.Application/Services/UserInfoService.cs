using EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;
using EnterpriseKnowledgeHub.Application.Exceptions;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipsForUser;
using Mediator;

namespace EnterpriseKnowledgeHub.Application.Services;

internal sealed class UserInfoService(IMediator _mediator) : IUserInfoService
{
    public async Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        var currentUser = await _mediator.Send(new ResolveCurrentUserCommand(), cancellationToken);

        // Every action other than /api/me requires a provisioned, invited user.
        if (currentUser.AccessDenied)
            throw new UserNotInvitedException("This account has not been invited and cannot be provisioned.");

        var membership = await _mediator.Send(new GetMembershipsForUserQuery(currentUser.Id), cancellationToken);

        return new UserInfo(currentUser.Id, membership.OrganizationIds);
    }
}
