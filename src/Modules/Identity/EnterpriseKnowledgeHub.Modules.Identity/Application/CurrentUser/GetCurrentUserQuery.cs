using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

public sealed record GetCurrentUserQuery() : IRequest<GetCurrentUserResult>;
