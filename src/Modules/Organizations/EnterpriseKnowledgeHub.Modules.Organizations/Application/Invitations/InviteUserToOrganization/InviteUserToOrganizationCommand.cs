using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.InviteUserToOrganization;

public sealed record InviteUserToOrganizationCommand(
    Guid OrganizationId,
    string Email) : IRequest<InviteUserToOrganizationResult>;
