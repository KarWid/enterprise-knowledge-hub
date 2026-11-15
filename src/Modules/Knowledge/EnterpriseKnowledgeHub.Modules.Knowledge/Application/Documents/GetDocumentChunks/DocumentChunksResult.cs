namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

internal sealed record DocumentChunkPreviewResult(Guid Id, int Ordinal, string Content, string? HeadingPath, int TokenCount, bool IsTruncated);
internal sealed record DocumentChunksResult(IReadOnlyList<DocumentChunkPreviewResult> Items, int ChunkCount, int Offset, bool HasMore);
