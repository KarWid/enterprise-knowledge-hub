using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;

public sealed record OrganizationMembership(
    Guid MembershipId,
    Guid OrganizationId,
    Guid UserId,
    string Role) : IOrganizationMembership;
