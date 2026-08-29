using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using MediatR;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    public sealed class UserContext(IMediator _mediator) : IUserContext
    {
        private Guid? _userId;

        public async Task<Guid> GetUserIdAsync(CancellationToken cancellationToken)
        {
            if (_userId.HasValue)
            {
                return _userId.Value;
            }

            var currentUser = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

            _userId = currentUser.Id;

            return _userId.Value;
        }
    }
}
