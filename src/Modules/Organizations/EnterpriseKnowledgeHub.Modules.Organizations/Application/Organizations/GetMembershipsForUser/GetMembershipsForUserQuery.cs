using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipsForUser;

public sealed record GetMembershipsForUserQuery(Guid UserId) : IRequest<GetMembershipsForUserQueryResult>;
