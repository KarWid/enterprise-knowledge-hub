using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed record DocumentResult(
    Guid Id,
    string Name,
    string ContentType,
    DocumentStatus Status,
    DateTime CreatedAt,
    string? ValidatedETag = null,
    DocumentTextExtractionResult? TextExtraction = null,
    DocumentChunkingResult? Chunking = null);
