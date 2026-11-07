using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "Documents";
    public const long DefaultMaxUploadSizeBytes = 25 * 1024 * 1024;
    public const int DefaultUploadSasLifetimeMinutes = 15;

    public long MaxUploadSizeBytes { get; init; } = DefaultMaxUploadSizeBytes;
    public int UploadSasLifetimeMinutes { get; init; } = DefaultUploadSasLifetimeMinutes;

    // Named flags support environment overrides without configuration-array merging surprises.
    public Dictionary<string, bool> EnabledFormats { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [DocumentFormat.Pdf.Id] = true,
        [DocumentFormat.Markdown.Id] = true
    };

    public bool IsEnabled(DocumentFormat format) => EnabledFormats.TryGetValue(format.Id, out var enabled) && enabled;

    public bool HasValidFormats() => EnabledFormats is not null &&
        EnabledFormats.Keys.All(id => DocumentFormat.FromId(id) is not null);
}
