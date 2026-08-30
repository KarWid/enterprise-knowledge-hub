using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.GetUserOrganizations;
using MediatR;

namespace EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview
{
    internal sealed class GetCurrentUserOverviewQueryHandler(IMediator _mediator) : IRequestHandler<GetCurrentUserOverviewQuery, GetCurrentUserOverviewQueryResult>
    {
        public async Task<GetCurrentUserOverviewQueryResult> Handle(GetCurrentUserOverviewQuery request, CancellationToken cancellationToken)
        {
            var currentUserResult = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
            var userOrganizationsResult = await _mediator.Send(new GetUserOrganizationsQuery(), cancellationToken);

            // TODO @KWidla
            var userOnboardingState = userOrganizationsResult?.Organizations?.Any() == true ? UserOnboardingState.Complete : UserOnboardingState.CreateOrganization;

            // TODO @KWidla: refactor
            return new GetCurrentUserOverviewQueryResult(currentUserResult.Id, currentUserResult.Email, currentUserResult.Email, userOnboardingState);
        }
    }
}
