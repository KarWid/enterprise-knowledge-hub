using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public interface IDocumentChunkingQueue
{
    Task<DocumentChunking?> ClaimAsync(CancellationToken cancellationToken);
    Task<int> CompleteAsync(DocumentChunking work, IReadOnlyList<DocumentChunkContent> chunks, CancellationToken cancellationToken);
    Task<int> FailAsync(DocumentChunking work, string errorCode, CancellationToken cancellationToken);
    Task<int> RetryAsync(DocumentChunking work, string errorCode, CancellationToken cancellationToken);
}
