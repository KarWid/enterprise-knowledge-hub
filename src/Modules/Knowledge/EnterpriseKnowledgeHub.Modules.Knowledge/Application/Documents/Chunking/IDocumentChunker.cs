using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public interface IDocumentChunker
{
    DocumentFormat Format { get; }
    string Version { get; }
    IReadOnlyList<DocumentChunkContent> Chunk(string text, string documentName, DocumentChunkingSettings settings, CancellationToken cancellationToken);
}

public sealed record DocumentChunkContent(int Ordinal, string Content, string? HeadingPath, int TokenCount);

public sealed class DocumentChunkingException(string errorCode) : Exception
{
    public string ErrorCode { get; } = errorCode;
}
