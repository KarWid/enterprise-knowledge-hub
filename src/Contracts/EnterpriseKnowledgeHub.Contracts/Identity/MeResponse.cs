namespace EnterpriseKnowledgeHub.Contracts.Identity;

public sealed record MeResponse(Guid Id, string? Email, string? Name);
