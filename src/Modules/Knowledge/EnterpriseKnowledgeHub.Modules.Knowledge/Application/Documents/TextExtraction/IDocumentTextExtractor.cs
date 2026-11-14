using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public interface IDocumentTextExtractor
{
    DocumentFormat Format { get; }
    string Version { get; }

    Task<string> ExtractAsync(Stream content, long contentLength, int maximumCharacters, CancellationToken cancellationToken);
}

// Permanent source/content failures have stable codes, never document text or Storage URLs.
public sealed class DocumentTextExtractionException(string errorCode) : Exception
{
    public string ErrorCode { get; } = errorCode;
}
