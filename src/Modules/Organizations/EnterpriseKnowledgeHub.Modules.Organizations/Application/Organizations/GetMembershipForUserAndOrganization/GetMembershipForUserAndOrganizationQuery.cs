using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;

public sealed record GetMembershipForUserAndOrganizationQuery(Guid UserId, Guid OrganizationId)
    : IRequest<GetMembershipForUserAndOrganizationResult>;
