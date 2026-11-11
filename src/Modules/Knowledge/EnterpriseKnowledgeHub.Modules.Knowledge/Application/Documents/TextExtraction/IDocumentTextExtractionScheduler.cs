using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public interface IDocumentTextExtractionScheduler
{
    // Stages work in the caller's upload-verification transaction; does not save independently.
    DocumentTextExtraction? Stage(Document document);
    Task ScheduleAsync(CancellationToken cancellationToken);
}
