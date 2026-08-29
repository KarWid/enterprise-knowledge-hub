using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.GetUserOrganizations;

public sealed record GetUserOrganizationsQuery() : IRequest<GetUserOrganizationsResult>;
