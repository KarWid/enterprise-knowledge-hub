using EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;
using Mediator;

namespace EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview
{
    internal sealed class GetCurrentUserOverviewQueryHandler(IMediator _mediator) : IRequestHandler<GetCurrentUserOverviewQuery, GetCurrentUserOverviewQueryResult>
    {
        public async ValueTask<GetCurrentUserOverviewQueryResult> Handle(GetCurrentUserOverviewQuery request, CancellationToken cancellationToken)
        {
            var currentUserResult = await _mediator.Send(new ResolveCurrentUserCommand(), cancellationToken);

            if (currentUserResult.AccessDenied)
            {
                return new GetCurrentUserOverviewQueryResult(
                    currentUserResult.Id,
                    currentUserResult.Email,
                    currentUserResult.Name,
                    UserOnboardingState.AccessDenied,
                    [],
                    []);
            }

            var userOrganizationsResult = await _mediator.Send(new GetUserOrganizationsQuery(), cancellationToken);
            var userOrganizations = userOrganizationsResult
                ?.Organizations
                ?.OrderBy(x => x.CreatedAt)
                ?.Select(o => new OrganizationOverviewItem(o.Id, o.Name, o.Role))
                .ToList() ?? [];

            var hasOrganizations = userOrganizations.Any();

            var pendingInvitations = hasOrganizations
                ? []
                : (await _mediator.Send(new GetPendingOrganizationInvitationsForEmailQuery(currentUserResult.Email), cancellationToken))
                    .Invitations
                    .Select(i => new PendingInvitationOverviewItem(i.Id, i.OrganizationId, i.OrganizationName, i.ExpiresAt))
                    .ToList();

            var userOnboardingState = hasOrganizations
                ? UserOnboardingState.Complete
                : pendingInvitations.Count > 0
                    ? UserOnboardingState.AcceptInvitation
                    : UserOnboardingState.CreateOrganization;

            return new GetCurrentUserOverviewQueryResult(
                currentUserResult.Id,
                currentUserResult.Email,
                currentUserResult.Name,
                userOnboardingState,
                pendingInvitations,
                userOrganizations);
        }
    }
}
