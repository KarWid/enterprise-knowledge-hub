namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public interface IUploadInbox
{
    Task EnqueueAsync(IEnumerable<UploadInboxItem> items, CancellationToken cancellationToken);
    Task<UploadInboxItem?> ClaimAsync(CancellationToken cancellationToken);
    Task<int> CompleteAsync(UploadInboxItem item, CancellationToken cancellationToken);
    Task<int> RetryAsync(UploadInboxItem item, string errorCode, CancellationToken cancellationToken);
}
