using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public sealed class DocumentTextExtractorRegistry(IEnumerable<IDocumentTextExtractor> extractors)
{
    private readonly IReadOnlyDictionary<string, IDocumentTextExtractor> byFormat = extractors
        .ToDictionary(extractor => extractor.Format.Id, StringComparer.OrdinalIgnoreCase);

    public IDocumentTextExtractor? Find(DocumentFormat format) => byFormat.GetValueOrDefault(format.Id);

    public bool SupportsEnabledFormats(DocumentTextExtractionOptions options) => options.IsValid() &&
        DocumentFormat.All.Where(options.IsEnabled).All(format => Find(format) is { Version.Length: > 0 and <= 64 });
}
