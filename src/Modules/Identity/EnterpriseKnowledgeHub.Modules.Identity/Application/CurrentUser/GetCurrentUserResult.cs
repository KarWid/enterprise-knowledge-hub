namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

public sealed record GetCurrentUserResult(string? Id, string? Email, string? Name);
