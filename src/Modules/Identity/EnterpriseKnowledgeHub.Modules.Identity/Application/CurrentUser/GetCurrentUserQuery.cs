using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

public sealed record GetCurrentUserQuery() : IRequest<GetCurrentUserResult>;
