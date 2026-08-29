using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.CreateOrganization;

public sealed record CreateOrganizationCommand(string Name) : IRequest<CreateOrganizationResult>;
