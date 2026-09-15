namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;

public sealed record PendingOrganizationInvitationItem(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    DateTime ExpiresAt);

public sealed record GetPendingOrganizationInvitationsForEmailResult(
    IReadOnlyList<PendingOrganizationInvitationItem> Invitations);
