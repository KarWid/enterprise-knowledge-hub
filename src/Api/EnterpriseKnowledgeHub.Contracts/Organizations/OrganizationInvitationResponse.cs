namespace EnterpriseKnowledgeHub.Contracts.Organizations;

public sealed record OrganizationInvitationResponse(
    Guid Id,
    Guid OrganizationId,
    string Email,
    DateTime ExpiresAt);
