namespace EnterpriseKnowledgeHub.Infrastructure.BlobStorage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string AccountUrl { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "documents";
}
