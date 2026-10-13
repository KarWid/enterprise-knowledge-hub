namespace EnterpriseKnowledgeHub.Api.StorageEvents;

public sealed class StorageEventOptions
{
    public const string SectionName = "StorageEvents";
    public bool Enabled { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SenderApplicationId { get; init; } = string.Empty;
    public string RequiredRole { get; init; } = "AzureEventGridSecureWebhookSubscriber";
    public string ExpectedSource { get; init; } = string.Empty;
    public int MaximumBatchSize { get; init; } = 100;
}
