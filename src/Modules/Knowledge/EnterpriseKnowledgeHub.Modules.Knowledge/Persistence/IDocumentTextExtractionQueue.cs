namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public interface IDocumentTextExtractionQueue
{
    Task<DocumentTextExtraction?> ClaimAsync(CancellationToken cancellationToken);
    Task<int> CompleteAsync(DocumentTextExtraction work, string text, CancellationToken cancellationToken);
    Task<int> FailAsync(DocumentTextExtraction work, string errorCode, CancellationToken cancellationToken);
    Task<int> RetryAsync(DocumentTextExtraction work, string errorCode, CancellationToken cancellationToken);
}
