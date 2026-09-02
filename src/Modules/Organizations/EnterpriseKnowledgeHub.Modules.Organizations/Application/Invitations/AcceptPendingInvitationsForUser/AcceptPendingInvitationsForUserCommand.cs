using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptPendingInvitationsForUser;

public sealed record AcceptPendingInvitationsForUserCommand(Guid UserId, string Email) : IRequest;
