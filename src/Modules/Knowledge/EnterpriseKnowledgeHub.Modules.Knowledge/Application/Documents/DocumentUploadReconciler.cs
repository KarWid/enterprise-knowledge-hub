using System.Linq.Expressions;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed class DocumentUploadReconciler(
    KnowledgeDbContext db, 
    IUploadInbox inbox, 
    TimeProvider clock, 
    IOptions<DocumentUploadWorkerOptions> options)
    : IDocumentUploadReconciler
{
    public async Task ScheduleAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await FindCandidatesAsync(now, ct);

        await inbox.EnqueueAsync(candidates.Select(candidate => CreateWork(candidate, now)), ct);
    }

    private Task<List<ReconciliationCandidate>> FindCandidatesAsync(DateTime now, CancellationToken ct)
    {
        var expiredBefore = now.AddSeconds(-options.Value.ReservationGraceSeconds);
        var legacyCreatedBefore = expiredBefore.AddMinutes(-DocumentUploadOptions.DefaultUploadSasLifetimeMinutes);

        return db.Documents
            .AsNoTracking()
            .Where(IsExpiredPendingUpload(expiredBefore, legacyCreatedBefore))
            .Where(DoesNotHaveReconciliationWork(db))
            .OrderBy(document => document.CreatedAt)
            .Take(options.Value.BatchSize)
            .Select(document => new ReconciliationCandidate(document.Id, document.BlobReference))
            .ToListAsync(ct);
    }

    private static Expression<Func<Document, bool>> IsExpiredPendingUpload(
        DateTime expiredBefore,
        DateTime legacyCreatedBefore) =>
        document =>
            document.Status == DocumentStatus.PendingForUpload &&
            (document.UploadExpiresAt <= expiredBefore ||
                (document.UploadExpiresAt == null && document.CreatedAt <= legacyCreatedBefore));

    private static Expression<Func<Document, bool>> DoesNotHaveReconciliationWork(KnowledgeDbContext db) =>
        document =>
            !db.UploadInbox.Any(work =>
                work.Kind == UploadWorkKind.Reconcile &&
                work.BlobReference == document.BlobReference);

    private static UploadInboxItem CreateWork(ReconciliationCandidate candidate, DateTime now) =>
        new()
        {
            Source = DocumentUploadWorkSources.Reconciliation,
            EventId = candidate.DocumentId.ToString("N"),
            BlobReference = candidate.BlobReference,
            Kind = UploadWorkKind.Reconcile, 
            CreatedAt = now, 
            NextAttemptAt = now
        };

    private sealed record ReconciliationCandidate(Guid DocumentId, string BlobReference);
}
