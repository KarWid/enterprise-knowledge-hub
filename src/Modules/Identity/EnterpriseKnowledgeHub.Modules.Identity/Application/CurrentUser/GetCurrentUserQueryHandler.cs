using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

// In Milestone 4 this handler will look up the ApplicationUser in the database.
internal sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    public Task<GetCurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetCurrentUserResult(request.ExternalId, request.Email, request.Name));
    }
}
