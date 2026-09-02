using EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using MediatR;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    public sealed class UserContext(IMediator _mediator) : IUserContext
    {
        private ResolveCurrentUserResult? _resolvedUser;

        // TODO @KWidla in the future, this could be extended to cache additional user context information - also should be contained within JWT token by custom claims
        public async Task<Guid> GetUserIdAsync(CancellationToken cancellationToken)
        {
            var resolvedUser = await ResolveAsync(cancellationToken);
            return resolvedUser.Id;
        }

        public async Task<string> GetUserEmailAsync(CancellationToken cancellationToken)
        {
            var resolvedUser = await ResolveAsync(cancellationToken);
            return resolvedUser.Email;
        }

        public async Task<string> GetUserNameAsync(CancellationToken cancellationToken)
        {
            var resolvedUser = await ResolveAsync(cancellationToken);
            return resolvedUser.Name;
        }

        private async Task<ResolveCurrentUserResult> ResolveAsync(CancellationToken cancellationToken)
        {
            if (_resolvedUser is not null)
            {
                return _resolvedUser;
            }

            _resolvedUser = await _mediator.Send(new ResolveCurrentUserCommand(), cancellationToken);

            return _resolvedUser;
        }
    }
}

