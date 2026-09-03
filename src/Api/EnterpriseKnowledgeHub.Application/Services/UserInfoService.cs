using EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipsForUser;
using MediatR;

namespace EnterpriseKnowledgeHub.Application.Services;

internal sealed class UserInfoService(IMediator _mediator) : IUserInfoService
{
    public async Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        var currentUser = await _mediator.Send(new ResolveCurrentUserCommand(), cancellationToken);
        var membership = await _mediator.Send(new GetMembershipsForUserQuery(currentUser.Id), cancellationToken);

        return new UserInfo(currentUser.Id, membership.OrganizationIds);
    }
}
