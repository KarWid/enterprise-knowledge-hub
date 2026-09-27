namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "Documents";
    public const long DefaultMaxUploadSizeBytes = 25 * 1024 * 1024;
    public const int DefaultUploadSasLifetimeMinutes = 15;

    public long MaxUploadSizeBytes { get; init; } = DefaultMaxUploadSizeBytes;
    public int UploadSasLifetimeMinutes { get; init; } = DefaultUploadSasLifetimeMinutes;
}
