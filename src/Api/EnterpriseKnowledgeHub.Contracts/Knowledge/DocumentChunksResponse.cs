using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentChunksResponse(
    [property: Required] IReadOnlyList<DocumentChunkPreviewResponse> Items,
    [property: Required] int ChunkCount, 
    [property: Required] int Offset, 
    [property: Required] bool HasMore);

public sealed record DocumentChunkPreviewResponse(
    [property: Required] Guid Id, 
    [property: Required] int Ordinal, 
    [property: Required] string Content,
    string? HeadingPath, 
    [property: Required] int TokenCount, 
    [property: Required] bool IsTruncated);
