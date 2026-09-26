using Mediator;

namespace EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;

public sealed record ResolveCurrentUserCommand() : IRequest<ResolveCurrentUserResult>;
