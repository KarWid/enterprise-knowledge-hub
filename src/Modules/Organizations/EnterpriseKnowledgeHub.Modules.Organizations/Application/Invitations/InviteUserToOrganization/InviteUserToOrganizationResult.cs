namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.InviteUserToOrganization;

public sealed record InviteUserToOrganizationResult(
    Guid Id,
    Guid OrganizationId,
    string Email,
    DateTime ExpiresAt);
