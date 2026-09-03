using EnterpriseKnowledgeHub.Application.Exceptions;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Application.ProvisionApplicationUser;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptPendingInvitationsForUser;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.HasPendingInvitationForEmail;
using MediatR;

namespace EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;

internal sealed class ResolveCurrentUserCommandHandler(IMediator _mediator, ICurrentUser _currentUser)
    : IRequestHandler<ResolveCurrentUserCommand, ResolveCurrentUserResult>
{
    public async Task<ResolveCurrentUserResult> Handle(
        ResolveCurrentUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.ExternalId))
            throw new InvalidOperationException("External identity ID is missing from the token.");

        var existingUser = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

        // TODO @KWidla: Consider caching the resolved user information to avoid repeated database calls within the same request.
        Guid userId;
        string email;
        string name;

        if (existingUser.Found)
        {
            userId = existingUser.Id;
            email = existingUser.Email ?? string.Empty;
            name = existingUser.Name ?? string.Empty;
        }
        else
        {
            email = _currentUser.Email ?? string.Empty;
            name = _currentUser.Name ?? string.Empty;

            var isInvited = await _mediator.Send(new HasPendingInvitationForEmailQuery(email), cancellationToken);
            if (!isInvited)
                throw new UserNotInvitedException("This account has not been invited and cannot be provisioned.");

            var provisioned = await _mediator.Send(
                new ProvisionApplicationUserCommand(_currentUser.ExternalId, email, name),
                cancellationToken);

            userId = provisioned.Id;
        }

        // Idempotent: also catches invitations issued after the user was first provisioned.
        await _mediator.Send(new AcceptPendingInvitationsForUserCommand(userId, email), cancellationToken);

        return new ResolveCurrentUserResult(userId, email, name);
    }
}
