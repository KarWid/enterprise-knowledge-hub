using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

// Internal machine workflow: ownership comes from the exact persisted reservation,
// never from event claims or an ambient current user/organization.
internal sealed class DocumentUploadFinalizer(
    KnowledgeDbContext db,
    IDocumentBlobStorage storage,
    IUploadInbox inbox,
    TimeProvider clock,
    IOptions<DocumentUploadOptions> uploadOptions,
    IOptions<DocumentUploadWorkerOptions> workerOptions,
    ILogger<DocumentUploadFinalizer> logger) : IDocumentUploadFinalizer
{
    public async Task ProcessAsync(UploadInboxItem work, CancellationToken ct)
    {
        var document = await db.Documents.SingleOrDefaultAsync(x => x.BlobReference == work.BlobReference, ct);
        if (document is null) return; // Never create documents or delete unmatched blobs.

        if (work.Kind == UploadWorkKind.Cleanup)
        {
            await CleanupAsync(document, ct);
            return;
        }

        if (document.Status is DocumentStatus.Rejected or DocumentStatus.Failed)
        {
            await inbox.EnqueueAsync([CleanupWork(document, work.Id.ToString("N"))], ct);
            return;
        }

        if (document.Status != DocumentStatus.PendingForUpload) return;

        DocumentUploadInspection? inspection;
        try
        {
            inspection = await storage.InspectUploadAsync(document.BlobReference, ct, work.ETag);
        }
        catch (DocumentBlobChangedException) when (work.ETag is not null)
        {
            // Stale event; the deadline reconciler will inspect the current committed blob.
            return;
        }

        if (inspection is null)
        {
            if (ReconcileAfter(document) > clock.GetUtcNow().UtcDateTime)
            {
                throw new DocumentUploadDeferredException("blob_not_yet_visible");
            }
                
            document.FailUpload();
            db.UploadInbox.Add(CleanupWork(document, document.Id.ToString("N")));
        }
        else if (inspection.ContentLength <= 0 || inspection.ContentLength > uploadOptions.Value.MaxUploadSizeBytes || !inspection.HasPdfSignature)
        {
            document.RejectUpload();
            // EF commits the rejection AND its cleanup job in one transaction.
            db.UploadInbox.Add(CleanupWork(document, document.Id.ToString("N")));
        }
        else
        {
            // Expired SAS does not invalidate bytes successfully committed to the reserved blob.
            document.CompleteUpload(inspection.ETag);
            // Uploaded means basic verification only. No extraction/indexing job is scheduled.
            // Future scheduling belongs here, behind DocumentProcessingGate, in this same commit.
        }

        await db.SaveChangesAsync(ct); // Revision protects event/reconciliation races.

        logger.LogInformation("Upload verification {EventId}: document {DocumentId} organization {OrganizationId} became {Status}.",
            work.EventId, document.Id, document.OrganizationId, document.Status);
    }

    private async Task CleanupAsync(Document document, CancellationToken ct)
    {
        if (document.Status is not (DocumentStatus.Rejected or DocumentStatus.Failed))
        {
            return;  
        } 

        if (ReconcileAfter(document) > clock.GetUtcNow().UtcDateTime)
        {
            throw new DocumentUploadDeferredException("cleanup_before_deadline");
        }

        // A rejected reservation remains rejected even if its blob was recreated with the SAS.
        // Read current properties, then delete only those exact bytes. A 412 retries the job.
        var inspection = await storage.InspectUploadAsync(document.BlobReference, ct);
        if (inspection is not null)
        {
            await storage.DeleteIfExistsAsync(document.BlobReference, inspection.ETag, ct);
        }
    }

    private UploadInboxItem CleanupWork(Document document, string eventId) => new()
    {
        Source = DocumentUploadWorkSources.Cleanup,
        EventId = eventId,
        BlobReference = document.BlobReference,
        Kind = UploadWorkKind.Cleanup,
        CreatedAt = clock.GetUtcNow().UtcDateTime,
        NextAttemptAt = ReconcileAfter(document)
    };

    private DateTime ReconcileAfter(Document document) =>
        (document.UploadExpiresAt ?? document.CreatedAt.AddMinutes(DocumentUploadOptions.DefaultUploadSasLifetimeMinutes))
            .AddSeconds(workerOptions.Value.ReservationGraceSeconds);
}
