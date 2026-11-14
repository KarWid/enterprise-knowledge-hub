namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public enum TextExtractionStatus { Pending, Processing, Completed, Failed, DeadLetter }

// One durable operation and result for the exact verified source. Text is never part of a work claim.
// The composite foreign key points to Document without CLR navigation properties: this keeps the
// independent lease/retry model out of the Document aggregate while the database enforces Organization ownership.
public sealed class DocumentTextExtraction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid DocumentId { get; set; }
    public string SourceETag { get; set; } = string.Empty;
    public string FormatId { get; set; } = string.Empty;
    public string ExtractorVersion { get; set; } = string.Empty;
    public TextExtractionStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public string? Text { get; set; }
    public int? CharacterCount { get; set; }
}
