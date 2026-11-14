using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed record DocumentResult(
    Guid Id,
    string Name,
    string ContentType,
    DocumentStatus Status,
    DateTime CreatedAt,
    string? ValidatedETag = null,
    DocumentTextExtractionResult? TextExtraction = null);
