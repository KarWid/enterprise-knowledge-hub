using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public interface IDocumentChunkingProcessor
{
    Task ProcessAsync(DocumentChunking work, CancellationToken cancellationToken);
}
