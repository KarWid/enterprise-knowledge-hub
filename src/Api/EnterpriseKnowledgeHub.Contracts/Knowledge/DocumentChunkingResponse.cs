using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentChunkingResponse(
    [property: Required] string Status, 
    int? ChunkCount, 
    DateTime? CompletedAt, 
    string? FailureCode);
