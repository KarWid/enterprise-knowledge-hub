using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.CompleteDocumentUpload;

internal sealed class CompleteDocumentUploadCommandHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService organizationAccessService,
    IDocumentBlobStorage blobStorage,
    IOptions<DocumentUploadOptions> uploadOptions,
    ILogger<CompleteDocumentUploadCommandHandler> logger)
    : IRequestHandler<CompleteDocumentUploadCommand, DocumentResult>
{
    public async ValueTask<DocumentResult> Handle(
        CompleteDocumentUploadCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService.GetCurrentOrganizationMembershipAsync(cancellationToken);

        var document = await db.Documents.SingleOrDefaultAsync(
            candidate => candidate.Id == request.DocumentId &&
                         candidate.OrganizationId == membership.OrganizationId,
            cancellationToken);

        if (document is null)
            throw new ArgumentException("The document upload was not found.");

        if (document.Status == DocumentStatus.Uploaded)
            return Map(document);

        if (document.Status != DocumentStatus.PendingForUpload)
            throw new ArgumentException("The document upload cannot be completed in its current state.");

        if (document.IsUploadExpired(DateTime.UtcNow))
        {
            await FailUploadAsync(document, deleteBlob: true);
            throw new ArgumentException("The document upload has expired. Start a new upload.");
        }

        var inspection = await blobStorage.InspectUploadAsync(document.BlobReference, cancellationToken);
        if (inspection is null)
        {
            await FailUploadAsync(document, deleteBlob: false);
            throw new ArgumentException("The document blob was not uploaded.");
        }

        if (!IsValidUpload(inspection, uploadOptions.Value))
        {
            await FailUploadAsync(document, deleteBlob: true);
            throw new ArgumentException("The uploaded file must be a PDF within the configured size limit.");
        }

        document.CompleteUpload();
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Document upload {DocumentId} was completed for organization {OrganizationId}.",
            document.Id,
            document.OrganizationId);

        return Map(document);
    }

    private static bool IsValidUpload(DocumentUploadInspection inspection, DocumentUploadOptions options)
    {
        if (options.MaxUploadSizeBytes <= 0)
            throw new InvalidOperationException("Documents:MaxUploadSizeBytes must be greater than zero.");

        return inspection.ContentLength is > 0 &&
               inspection.ContentLength <= options.MaxUploadSizeBytes &&
               inspection.HasPdfSignature;
    }

    private async Task FailUploadAsync(Document document, bool deleteBlob)
    {
        document.FailUpload();
        await db.SaveChangesAsync(CancellationToken.None);

        if (!deleteBlob)
            return;

        try
        {
            await blobStorage.DeleteIfExistsAsync(document.BlobReference, CancellationToken.None);
        }
        catch (Exception exception)
        {
            // TODO @KWidla: Consider implementing a retry mechanism or alerting system for failed blob deletions.
            logger.LogError(
                exception,
                "Failed to remove invalid document blob {DocumentId}.",
                document.Id);
        }
    }

    private static DocumentResult Map(Document document) => new(
        document.Id,
        document.Name,
        document.ContentType,
        document.Status,
        document.CreatedAt);
}
