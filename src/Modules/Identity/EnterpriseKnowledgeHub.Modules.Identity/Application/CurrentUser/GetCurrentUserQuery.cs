using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

public sealed record GetCurrentUserQuery(
    string? ExternalId,
    string? Email,
    string? Name) : IRequest<GetCurrentUserResult>;
