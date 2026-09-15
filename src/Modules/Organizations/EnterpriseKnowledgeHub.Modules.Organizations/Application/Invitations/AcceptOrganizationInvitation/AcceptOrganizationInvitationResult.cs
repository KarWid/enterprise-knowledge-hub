namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptOrganizationInvitation;

public sealed record AcceptOrganizationInvitationResult(Guid OrganizationId, string OrganizationName, string Role);
