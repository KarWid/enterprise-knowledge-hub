using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed record DocumentResult(
    Guid Id,
    string Name,
    string ContentType,
    DocumentStatus Status,
    DateTime CreatedAt);
