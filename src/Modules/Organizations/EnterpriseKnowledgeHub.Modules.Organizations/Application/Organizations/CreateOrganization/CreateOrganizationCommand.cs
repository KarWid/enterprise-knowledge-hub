using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(string Name) : IRequest<CreateOrganizationResult>;
