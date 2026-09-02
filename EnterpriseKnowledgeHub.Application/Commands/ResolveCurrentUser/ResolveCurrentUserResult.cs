namespace EnterpriseKnowledgeHub.Application.Commands.ResolveCurrentUser;

public sealed record ResolveCurrentUserResult(Guid Id, string Email, string Name);
