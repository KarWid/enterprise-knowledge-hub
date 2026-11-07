using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

public sealed class Document
{
    private const int MaximumNameLength = 256;

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string BlobReference { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UploadExpiresAt { get; private set; }
    public string? ValidatedETag { get; private set; }
    public string? FailureCode { get; private set; }
    public Guid Revision { get; private set; } = Guid.NewGuid();

    private Document()
    {
    }

    public static Document CreatePendingUpload(
        Guid organizationId,
        string name,
        Guid createdBy,
        DateTime uploadExpiresAt)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId is required.");

        if (createdBy == Guid.Empty)
            throw new DomainException("CreatedBy is required.");

        if (uploadExpiresAt <= DateTime.UtcNow)
            throw new DomainException("Upload expiration must be in the future.");

        var normalizedName = DomainGuard.Required(name, nameof(Name), MaximumNameLength).Trim();
        var format = DocumentFormat.FromFileName(normalizedName)
            ?? throw new DomainException("Unsupported document extension.");

        var id = Guid.NewGuid();

        return new Document
        {
            Id = id,
            OrganizationId = organizationId,
            Name = normalizedName,
            BlobReference = $"organizations/{organizationId:N}/documents/{id:N}{format.Extension}",
            ContentType = format.ContentType,
            Status = DocumentStatus.PendingForUpload,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UploadExpiresAt = uploadExpiresAt
        };
    }

    public bool IsUploadExpired(DateTime utcNow) =>
        Status == DocumentStatus.PendingForUpload &&
        (UploadExpiresAt is null || UploadExpiresAt <= utcNow);

    public void CompleteUpload(string etag)
    {
        EnsurePendingUpload();

        ValidatedETag = DomainGuard.Required(etag, nameof(ValidatedETag), 128);
        Status = DocumentStatus.Uploaded;
        Revision = Guid.NewGuid();
    }

    public void FailUpload(string failureCode = "upload_missing")
    {
        EnsurePendingUpload();

        Status = DocumentStatus.Failed;
        FailureCode = failureCode;
        Revision = Guid.NewGuid();
    }

    public void RejectUpload(string failureCode = "invalid_format_or_size")
    {
        EnsurePendingUpload();
        Status = DocumentStatus.Rejected;
        FailureCode = failureCode;
        Revision = Guid.NewGuid();
    }

    private void EnsurePendingUpload()
    {
        if (Status != DocumentStatus.PendingForUpload)
            throw new DomainException("Only a pending document upload can change upload status.");
    }
}
