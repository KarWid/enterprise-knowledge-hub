using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptOrganizationInvitation;

public sealed record AcceptOrganizationInvitationCommand(Guid InvitationId)
    : IRequest<AcceptOrganizationInvitationResult>;
