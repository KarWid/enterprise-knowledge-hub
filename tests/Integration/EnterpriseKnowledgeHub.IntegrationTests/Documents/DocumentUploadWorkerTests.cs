using EnterpriseKnowledgeHub.Infrastructure.DocumentUploads;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

/// <summary>
/// Exercises one controlled batch of the real document upload background worker.
/// The tests use SQLite for durable inbox state, a controllable clock for leases
/// and retry delays, and test Blob Storage behavior supplied by UploadTestDatabase.
/// </summary>
/// <remarks>
/// RunBatchAsync is invoked directly so the tests do not start an uncontrolled
/// polling loop or wait for real time to pass.
/// </remarks>
public sealed class DocumentUploadWorkerTests
{
    private const string SanitizedDependencyFailure = "dependency_failure";

    [Fact]
    public async Task TransientStorageFailure_IsRetriedAndUploadCompletesOnceAfterRecovery()
    {
        // Arrange
        // Persist both a pending Document reservation and its corresponding inbox work.
        using var fixture = new UploadTestDatabase();
        await using (var arrangeDb = fixture.CreateContext())
        {
            var document = await UploadFinalizationTests.Reserve(arrangeDb);
            var workItem = UploadFinalizationTests.Work(document);

            workItem.Source = "storage";

            // Make the item immediately eligible for the worker's first claim.
            workItem.NextAttemptAt = fixture.Clock.Now.UtcDateTime;
            await fixture.Inbox(arrangeDb).EnqueueAsync([workItem], CancellationToken.None);
        }

        // Build the same scoped dependency graph used by the hosted worker.
        using var provider = CreateServiceProvider(fixture);
        using var worker = CreateWorker(fixture, provider);

        // Simulate a transient storage outage. The original message deliberately contains
        // sensitive-looking text to prove that only a sanitized error code is persisted.
        fixture.Storage.InspectError = new IOException("secret data must never be persisted");

        // Act - first attempt
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert - first attempt
        // The failure must return the item to Pending and schedule a durable retry.
        await using (var retryAssertDb = fixture.CreateContext())
        {
            var pendingWork = await retryAssertDb.UploadInbox.AsNoTracking().SingleAsync();
            Assert.Equal(UploadWorkStatus.Pending, pendingWork.Status);
            Assert.Equal(1, pendingWork.Attempts);
            Assert.Equal(SanitizedDependencyFailure, pendingWork.LastError);
        }

        // Arrange - retry
        // Move the test clock to NextAttemptAt and restore the storage dependency.
        fixture.Clock.Advance(fixture.Options.RetryBaseSeconds);
        fixture.Storage.InspectError = null;

        // Act - retry
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert - retry
        // Read with a fresh DbContext so assertions observe committed database state.
        await using var completionAssertDb = fixture.CreateContext();
        var completedWork = await completionAssertDb.UploadInbox.AsNoTracking().SingleAsync();
        var uploadedDocument = await completionAssertDb.Documents.AsNoTracking().SingleAsync();

        Assert.Equal(UploadWorkStatus.Completed, completedWork.Status);
        Assert.Equal(2, completedWork.Attempts);
        Assert.Equal(DocumentStatus.Uploaded, uploadedDocument.Status);
        Assert.NotNull(uploadedDocument.ValidatedETag);
        Assert.True(
            fixture.Storage.Inspections == 2,
            "Storage must be inspected once during the failed attempt and once during the successful retry.");
    }

    [Fact]
    public async Task ExpiredLease_AfterMaximumAttempts_IsDeadLetteredWithoutProcessingAgain()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using (var arrangeDb = fixture.CreateContext())
        {
            var abandonedWork = UploadInboxTests.Item(fixture);

            // Processing with an expired lease represents work abandoned by a crashed worker.
            abandonedWork.Status = UploadWorkStatus.Processing;
            abandonedWork.LeaseOwner = Guid.NewGuid();
            abandonedWork.LeaseExpiresAt = fixture.Clock.Now.UtcDateTime.AddSeconds(-1);

            // The crashed worker had already consumed the final permitted processing attempt.
            abandonedWork.Attempts = fixture.Options.MaxAttempts;
            await fixture.Inbox(arrangeDb).EnqueueAsync([abandonedWork], CancellationToken.None);
        }

        using var provider = CreateServiceProvider(fixture);
        using var worker = CreateWorker(fixture, provider);

        // Act
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert
        // Claiming the expired lease increments Attempts once more. The worker detects that
        // the recovery budget is exhausted and dead-letters it without touching Blob Storage.
        await using var assertDb = fixture.CreateContext();
        var deadLetteredWork = await assertDb.UploadInbox.AsNoTracking().SingleAsync();

        Assert.Equal(UploadWorkStatus.DeadLetter, deadLetteredWork.Status);
        Assert.Equal(fixture.Options.MaxAttempts + 1, deadLetteredWork.Attempts);
        Assert.Equal("lease_recovery_exhausted", deadLetteredWork.LastError);
        Assert.True(
            fixture.Storage.Inspections == 0,
            "Exhausted recovered work must be dead-lettered before document or Blob Storage processing starts.");
    }

    private static ServiceProvider CreateServiceProvider(UploadTestDatabase fixture)
    {
        var services = new ServiceCollection();

        // The worker resolves these singleton policies and test dependencies in every scope.
        services.AddLogging();
        services.AddSingleton<TimeProvider>(fixture.Clock);
        services.AddSingleton<IDocumentBlobStorage>(fixture.Storage);
        services.AddSingleton(Options.Create(fixture.Options));
        services.AddSingleton(Options.Create(new DocumentUploadOptions()));

        // Each worker iteration receives a fresh DbContext, matching application DI lifetime.
        services.AddScoped(_ => fixture.CreateContext());

        // Use the real inbox, finalizer, and reconciler implementations under test.
        services.AddScoped<IUploadInbox, UploadInbox>();
        services.AddScoped<IDocumentUploadFinalizer, DocumentUploadFinalizer>();
        services.AddScoped<IDocumentUploadReconciler, DocumentUploadReconciler>();

        return services.BuildServiceProvider();
    }

    private static DocumentUploadWorker CreateWorker(
        UploadTestDatabase fixture,
        ServiceProvider provider)
    {
        return new DocumentUploadWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(fixture.Options),
            NullLogger<DocumentUploadWorker>.Instance);
    }
}
