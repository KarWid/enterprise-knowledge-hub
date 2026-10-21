namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public enum UploadWorkKind { Verify, Reconcile, Cleanup }
public enum UploadWorkStatus { Pending, Processing, Completed, DeadLetter }

// Infrastructure data, not a domain aggregate. No event payload, URL, or SAS is stored.
public sealed class UploadInboxItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string BlobReference { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public UploadWorkKind Kind { get; set; }
    public UploadWorkStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
