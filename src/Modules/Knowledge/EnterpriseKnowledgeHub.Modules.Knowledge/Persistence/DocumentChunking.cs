namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public enum DocumentChunkingStatus { Pending, Processing, Completed, Failed, DeadLetter }

// A durable operation and immutable result set for one extraction and configuration.
public sealed class DocumentChunking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ExtractionId { get; set; }
    public string SourceETag { get; set; } = string.Empty;
    public string FormatId { get; set; } = string.Empty;
    public string ChunkerVersion { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public int TargetTokens { get; set; }
    public int MaxTokens { get; set; }
    public int OverlapTokens { get; set; }
    public int MaxChunksPerDocument { get; set; }
    public int MaxSourceCharacters { get; set; }
    public DocumentChunkingStatus Status { get; set; }
    public int? ChunkCount { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
