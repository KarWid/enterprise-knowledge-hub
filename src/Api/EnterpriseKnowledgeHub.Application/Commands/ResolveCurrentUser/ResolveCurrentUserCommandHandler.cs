using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Application.ProvisionApplicationUser;
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

        if (existingUser.Found)
        {
            return new ResolveCurrentUserResult(existingUser.Id, existingUser.Email ?? string.Empty, existingUser.Name ?? string.Empty);
        }

        var email = _currentUser.Email ?? string.Empty;
        var name = _currentUser.Name ?? string.Empty;

        var isInvited = await _mediator.Send(new HasPendingInvitationForEmailQuery(email), cancellationToken);
        if (!isInvited)
            return new ResolveCurrentUserResult(Guid.Empty, email, name, AccessDenied: true);

        var provisioned = await _mediator.Send(
            new ProvisionApplicationUserCommand(_currentUser.ExternalId, email, name),
            cancellationToken);

        return new ResolveCurrentUserResult(provisioned.Id, email, name);
    }
}