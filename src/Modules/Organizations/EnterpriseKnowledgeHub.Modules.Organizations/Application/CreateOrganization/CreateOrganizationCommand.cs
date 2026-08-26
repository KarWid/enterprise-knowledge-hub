using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.CreateOrganization;

public sealed record CreateOrganizationCommand(Guid UserId, string Name) : IRequest<CreateOrganizationResult>;
