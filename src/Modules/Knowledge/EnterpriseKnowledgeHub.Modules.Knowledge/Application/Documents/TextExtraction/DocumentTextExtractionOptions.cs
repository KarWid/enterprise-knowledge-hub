using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public sealed class DocumentTextExtractionOptions
{
    public const string SectionName = "DocumentTextExtraction";
    public const int PreviewCharacterLimit = 4000;

    public long MaxSourceSizeBytes { get; init; } = 25 * 1024 * 1024;
    public int MaxTextCharacters { get; init; } = 5_000_000;
    public Dictionary<string, bool> EnabledFormats { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [DocumentFormat.Markdown.Id] = true,
        [DocumentFormat.Pdf.Id] = false
    };

    public bool IsEnabled(DocumentFormat format) => EnabledFormats.TryGetValue(format.Id, out var enabled) && enabled;

    public bool IsValid() =>
        MaxSourceSizeBytes is > 0 and <= 524288000 &&
        MaxTextCharacters is > 0 and <= 50_000_000 &&
        EnabledFormats is not null &&
        EnabledFormats.Keys.All(id => DocumentFormat.FromId(id) is not null);
}
