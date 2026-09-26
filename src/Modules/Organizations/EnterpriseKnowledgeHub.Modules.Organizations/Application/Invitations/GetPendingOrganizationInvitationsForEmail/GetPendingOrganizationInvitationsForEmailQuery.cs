using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;

public sealed record GetPendingOrganizationInvitationsForEmailQuery(string Email)
    : IRequest<GetPendingOrganizationInvitationsForEmailResult>;
