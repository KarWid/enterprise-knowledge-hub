using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;

public sealed record GetMembershipForUserAndOrganizationResult(bool Found, Guid MembershipId, OrganizationRole Role);
