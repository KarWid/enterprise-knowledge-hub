using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public interface IDocumentTextExtractionProcessor
{
    Task ProcessAsync(DocumentTextExtraction work, CancellationToken cancellationToken);
}
