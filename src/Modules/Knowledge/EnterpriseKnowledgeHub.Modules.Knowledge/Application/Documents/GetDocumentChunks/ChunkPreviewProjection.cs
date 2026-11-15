namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

internal sealed record ChunkPreviewProjection(Guid Id, int Ordinal, string Content, string? HeadingPath, int TokenCount);
