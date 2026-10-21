using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

/// <summary>
/// Exercises document upload finalization against the real EF Core persistence layer.
/// Blob Storage behavior and time are controlled by UploadTestDatabase so validation,
/// retries, cleanup, reconciliation, idempotency, and concurrency can be deterministic.
/// </summary>
public sealed class UploadFinalizationTests
{
    private const string InitialETag = "\"0x123\"";
    private const string RecreatedBlobETag = "\"0x456\"";
    private const int AfterDeadlineSeconds = 3_600;

    [Fact]
    public async Task ValidUpload_AfterSasExpiry_IsFinalizedOnceAndPersistsETag()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);

        // SAS expiry must not invalidate a blob that was successfully committed before processing.
        fixture.Clock.Advance(AfterDeadlineSeconds);
        var workItem = Work(document);
        var finalizer = fixture.Finalizer(db);

        // Act
        await finalizer.ProcessAsync(workItem, CancellationToken.None);
        var revisionAfterFirstFinalization = document.Revision;

        // Event Grid can redeliver the same event, so process it a second time.
        await finalizer.ProcessAsync(workItem, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.Equal(fixture.Storage.Inspection!.ETag, document.ValidatedETag);
        Assert.Equal(revisionAfterFirstFinalization, document.Revision);
        Assert.True(
            fixture.Storage.Inspections == 1,
            "A duplicate event for an already uploaded document must not inspect Blob Storage again.");
        Assert.False(
            DocumentProcessingGate.CanStartExtraction(document),
            "Basic upload verification must not start extraction before future processing gates exist.");
        Assert.Empty(await db.UploadInbox.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(0, false)]
    [InlineData(26_214_401, true)]
    public async Task InvalidUpload_IsRejectedAndSchedulesDelayedConditionalCleanup(long contentLength, bool hasPdfSignature)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);
        fixture.Storage.Inspection = new DocumentUploadInspection(
            contentLength,
            hasPdfSignature,
            InitialETag);
            
        var finalizer = fixture.Finalizer(db);

        // Act
        // Reject the invalid upload and atomically schedule cleanup.
        await finalizer.ProcessAsync(Work(document), CancellationToken.None);
        var cleanupWork = await db.UploadInbox.AsNoTracking().SingleAsync();
        var cleanupClaimBeforeDeadline = await fixture.Inbox(db).ClaimAsync(CancellationToken.None);

        // A transient deletion failure must leave the rejection and cleanup retryable.
        fixture.Clock.Advance(AfterDeadlineSeconds);
        fixture.Storage.FailDeletion = true;
        var deletionFailure = await Record.ExceptionAsync(
            () => finalizer.ProcessAsync(cleanupWork, CancellationToken.None));

        // The create-only SAS could recreate the blob before expiry. Cleanup must inspect
        // its current ETag and conditionally delete those exact bytes, not the old version.
        fixture.Storage.FailDeletion = false;
        fixture.Storage.Inspection = new DocumentUploadInspection(
            ContentLength: 100,
            HasPdfSignature: true,
            ETag: RecreatedBlobETag);

        await finalizer.ProcessAsync(cleanupWork, CancellationToken.None);

        // Reprocessing after a successful delete simulates a worker crash before inbox completion.
        await finalizer.ProcessAsync(cleanupWork, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentStatus.Rejected, document.Status);
        Assert.True(
            cleanupWork.NextAttemptAt > document.UploadExpiresAt,
            "Cleanup must wait until the upload deadline and grace period have passed.");
        Assert.True(
            cleanupClaimBeforeDeadline is null,
            "Cleanup must remain unclaimable until the upload SAS deadline and reservation grace period have passed.");
        Assert.IsType<IOException>(deletionFailure);
        Assert.Equal(RecreatedBlobETag, Assert.Single(fixture.Storage.Deletions).ETag);
    }

    [Fact]
    public async Task TransientStorageFailure_LeavesReservationPendingWithoutDeletingBlob()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);
        fixture.Storage.InspectError = new IOException("Simulated Blob Storage outage.");
        fixture.Clock.Advance(AfterDeadlineSeconds);
        var finalizer = fixture.Finalizer(db);

        // Act
        Task FinalizeUpload() => finalizer.ProcessAsync(Work(document), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<IOException>(FinalizeUpload);
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.Empty(fixture.Storage.Deletions);
    }

    [Fact]
    public async Task Cleanup_BeforeDeadline_IsDeferredWithoutCallingStorage()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);
        document.RejectUpload();
        await db.SaveChangesAsync();

        var cleanupWork = new UploadInboxItem
        {
            BlobReference = document.BlobReference,
            Kind = UploadWorkKind.Cleanup
        };

        var finalizer = fixture.Finalizer(db);

        // Act
        Task CleanupBeforeDeadline() => finalizer.ProcessAsync(
            cleanupWork,
            CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DocumentUploadDeferredException>(
            CleanupBeforeDeadline);
        Assert.Equal("cleanup_before_deadline", exception.ErrorCode);
        Assert.True(
            fixture.Storage.Inspections == 0,
            "Deferred cleanup must not inspect Blob Storage before the reservation deadline.");
        Assert.Empty(fixture.Storage.Deletions);
    }

    [Fact]
    public async Task UnmatchedEvent_DoesNotInspectStorageOrChangeDocument()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);
        var unmatchedWork = new UploadInboxItem
        {
            BlobReference = "unmatched.pdf",
            Kind = UploadWorkKind.Verify
        };

        // Act
        await fixture.Finalizer(db).ProcessAsync(unmatchedWork, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.True(
            fixture.Storage.Inspections == 0,
            "An event without an exact reservation must not cause any Blob Storage access.");
        Assert.Empty(fixture.Storage.Deletions);
    }

    [Fact]
    public async Task StaleEvent_WithDifferentETag_DoesNotChangeDocumentOrDeleteBlob()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);
        var staleWork = Work(document);
        staleWork.ETag = RecreatedBlobETag;

        // Act
        await fixture.Finalizer(db).ProcessAsync(staleWork, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.True(
            fixture.Storage.Inspections == 1,
            "The finalizer must inspect using the event ETag before identifying a stale event.");
        Assert.Empty(fixture.Storage.Deletions);
    }

    /// <summary>
    /// Verifies the recovery path for a pending reservation whose BlobCreated event was
    /// delayed or never delivered. After the upload deadline and grace period, the
    /// application must inspect the reserved blob instead of treating SAS expiry as proof
    /// that the upload failed.
    /// </summary>
    /// <remarks>
    /// A valid existing blob becomes Uploaded even though its SAS has expired. A genuinely
    /// absent blob becomes Failed. Scheduling reconciliation repeatedly must remain
    /// idempotent and create only one durable work item for the reservation.
    /// </remarks>
    /// <param name="blobExists">
    /// Whether Blob Storage contains the successfully committed reserved blob.
    /// </param>
    /// <param name="expectedStatus">
    /// The document status expected after reconciliation inspects the actual blob state.
    /// </param>
    [Theory]
    [InlineData(true, DocumentStatus.Uploaded)]
    [InlineData(false, DocumentStatus.Failed)]
    public async Task Reconciliation_AfterDeadline_UsesBlobExistenceRatherThanSasExpiry(
        bool blobExists,
        DocumentStatus expectedStatus)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await Reserve(db);

        if (!blobExists)
        {
            fixture.Storage.Inspection = null;
        }

        var inbox = fixture.Inbox(db);
        var reconciler = new DocumentUploadReconciler(
            db,
            inbox,
            fixture.Clock,
            Options.Create(fixture.Options));

        // Act
        // No reconciliation work should be scheduled before the deadline.
        await reconciler.ScheduleAsync(CancellationToken.None);
        var workItemsBeforeDeadline = await db.UploadInbox.AsNoTracking().CountAsync();

        // After the deadline, repeated scheduling must produce one idempotent work item.
        fixture.Clock.Advance(AfterDeadlineSeconds);
        await reconciler.ScheduleAsync(CancellationToken.None);
        await reconciler.ScheduleAsync(CancellationToken.None);
        var reconciliationWork = await db.UploadInbox.AsNoTracking().SingleAsync();

        // The finalizer decides from actual Blob Storage state, not merely from SAS expiry.
        await fixture.Finalizer(db).ProcessAsync(reconciliationWork, CancellationToken.None);

        // Assert
        Assert.Equal(0, workItemsBeforeDeadline);
        Assert.Equal(DocumentUploadWorkSources.Reconciliation, reconciliationWork.Source);
        Assert.Equal(expectedStatus, document.Status);
    }

    /// <summary>
    /// Verifies that a BlobCreated event and deadline reconciliation cannot commit
    /// conflicting terminal transitions for the same pending document.
    /// </summary>
    /// <remarks>
    /// The test deterministically simulates the race with two independent DbContext
    /// instances that load the same document revision. The event commits Uploaded first.
    /// Reconciliation then tries to commit Rejected using its stale revision, which must
    /// produce a concurrency exception. The losing transaction must not persist its
    /// associated cleanup work, and the document must remain Uploaded.
    /// </remarks>
    [Fact]
    public async Task EventAndReconciliation_WhenTheyRace_ConcurrencyTokenPreventsConflictingTransition()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var eventDb = fixture.CreateContext();
        var document = await Reserve(eventDb);

        // A second unit of work loads the same Pending document before the event completes it.
        await using var reconciliationDb = fixture.CreateContext();
        var staleDocument = await reconciliationDb.Documents.SingleAsync();
        var staleReconciliationWork = new UploadInboxItem
        {
            BlobReference = staleDocument.BlobReference,
            Kind = UploadWorkKind.Reconcile
        };

        // Act
        // The storage event wins and commits Uploaded first.
        await fixture.Finalizer(eventDb).ProcessAsync(Work(document), CancellationToken.None);

        // The stale reconciliation now sees invalid content and tries to commit Rejected
        // using the old Revision value that it loaded before event finalization.
        fixture.Storage.Inspection = new DocumentUploadInspection(
            ContentLength: 100,
            HasPdfSignature: false,
            ETag: InitialETag);
            
        Task CommitStaleRejection() => fixture.Finalizer(reconciliationDb).ProcessAsync(
            staleReconciliationWork,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(CommitStaleRejection);

        // Clear the rejected entity and uncommitted cleanup item left in the losing context,
        // then verify the database contains only the winning Uploaded transition.
        reconciliationDb.ChangeTracker.Clear();
        var persistedDocument = await reconciliationDb.Documents.AsNoTracking().SingleAsync();
        var cleanupItems = await reconciliationDb.UploadInbox.AsNoTracking().ToListAsync();

        Assert.Equal(DocumentStatus.Uploaded, persistedDocument.Status);
        Assert.Empty(cleanupItems);
    }

    internal static async Task<Document> Reserve(KnowledgeDbContext db)
    {
        var document = Document.CreatePendingUpload(
            Guid.NewGuid(),
            "regulations.pdf",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15));

        db.Documents.Add(document);

        await db.SaveChangesAsync();

        return document;
    }

    internal static UploadInboxItem Work(Document document)
    {
        return new UploadInboxItem
        {
            BlobReference = document.BlobReference,
            ETag = InitialETag,
            EventId = Guid.NewGuid().ToString(),
            Kind = UploadWorkKind.Verify
        };
    }
}
