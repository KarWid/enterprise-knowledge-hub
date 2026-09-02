using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;

public sealed record GetUserOrganizationsQuery() : IRequest<GetUserOrganizationsResult>;
