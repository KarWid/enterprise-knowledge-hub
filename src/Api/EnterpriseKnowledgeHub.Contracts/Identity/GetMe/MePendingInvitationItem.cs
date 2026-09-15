namespace EnterpriseKnowledgeHub.Contracts.Identity.GetMe;

public sealed record MePendingInvitationItem(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    DateTime ExpiresAt);
