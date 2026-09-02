using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.HasPendingInvitationForEmail;

public sealed record HasPendingInvitationForEmailQuery(string Email) : IRequest<bool>;
