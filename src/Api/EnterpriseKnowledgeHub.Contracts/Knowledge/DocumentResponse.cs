namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentResponse(
    Guid Id,
    string Name,
    string ContentType,
    string Status,
    DateTime CreatedAt);
