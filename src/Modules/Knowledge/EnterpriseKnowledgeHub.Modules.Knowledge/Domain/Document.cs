using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

public sealed class Document
{
    private const int MaximumNameLength = 256;
    private const string PdfContentType = "application/pdf";

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string BlobReference { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UploadExpiresAt { get; private set; }

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
        if (!string.Equals(Path.GetExtension(normalizedName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Only PDF documents are supported.");

        var id = Guid.NewGuid();

        return new Document
        {
            Id = id,
            OrganizationId = organizationId,
            Name = normalizedName,
            BlobReference = $"organizations/{organizationId:N}/documents/{id:N}.pdf",
            ContentType = PdfContentType,
            Status = DocumentStatus.PendingForUpload,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UploadExpiresAt = uploadExpiresAt
        };
    }

    public bool IsUploadExpired(DateTime utcNow) =>
        Status == DocumentStatus.PendingForUpload &&
        (UploadExpiresAt is null || UploadExpiresAt <= utcNow);

    public void CompleteUpload()
    {
        EnsurePendingUpload();

        Status = DocumentStatus.Uploaded;
        UploadExpiresAt = null;
    }

    public void FailUpload()
    {
        EnsurePendingUpload();

        Status = DocumentStatus.Failed;
        UploadExpiresAt = null;
    }

    private void EnsurePendingUpload()
    {
        if (Status != DocumentStatus.PendingForUpload)
            throw new DomainException("Only a pending document upload can change upload status.");
    }
}
