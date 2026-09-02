using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.ProvisionApplicationUser;

public sealed record ProvisionApplicationUserCommand(
    string ExternalIdentityId,
    string Email,
    string DisplayName) : IRequest<ProvisionApplicationUserResult>;
