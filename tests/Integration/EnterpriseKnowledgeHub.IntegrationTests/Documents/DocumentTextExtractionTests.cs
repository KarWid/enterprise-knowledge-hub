using System.Data.Common;
using System.Text;
using EnterpriseKnowledgeHub.Infrastructure.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentTextPreview;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class DocumentTextExtractionTests
{
    [Theory]
    [InlineData("notes.md", 1)]
    [InlineData("manual.pdf", 0)]
    public async Task VerifiedUpload_SchedulesEnabledExtractionOnce(string fileName, int expectedWorkCount)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await ReserveAsync(db, fileName);
        var finalizer = fixture.Finalizer(db);
        var notification = Notification(document);

        // Act
        await finalizer.ProcessAsync(notification, CancellationToken.None);
        await finalizer.ProcessAsync(notification, CancellationToken.None);
        await fixture.ExtractionScheduler(db).ScheduleAsync(CancellationToken.None);

        // Assert
        var extractions = await db.DocumentTextExtractions.AsNoTracking().ToListAsync();
        Assert.Equal(expectedWorkCount, extractions.Count);
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.All(extractions, extraction =>
        {
            Assert.Equal(document.OrganizationId, extraction.OrganizationId);
            Assert.Equal(document.ValidatedETag, extraction.SourceETag);
            Assert.Equal(TextExtractionStatus.Pending, extraction.Status);
        });
    }

    [Fact]
    public async Task VerificationFailure_RollsBackUploadAndExtractionSchedulingTogether()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setup = fixture.CreateContext();
        var document = await ReserveAsync(setup);
        var failingOptions = fixture
            .Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(new RejectExtractionInsert()).Options;
        await using var failingDb = new KnowledgeDbContext(failingOptions);

        // Act
        var verify = () => fixture.Finalizer(failingDb).ProcessAsync(Notification(document), CancellationToken.None);

        // Assert
        await Assert.ThrowsAnyAsync<Exception>(verify);
        await using var persisted = fixture.CreateContext();
        Assert.Equal(DocumentStatus.PendingForUpload, (await persisted.Documents.SingleAsync()).Status);
        Assert.Empty(await persisted.DocumentTextExtractions.ToListAsync());
    }

    [Fact]
    public async Task Extraction_PersistsLargeNormalizedTextAndBoundedPreview()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        fixture.Storage.TextBytes = Encoding.UTF8.GetBytes("\uFEFF# Notes\r\n" + new string('a', 1_048_576));
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);

        // Act
        await fixture.ExtractionProcessor(db).ProcessAsync(work!, CancellationToken.None);
        var preview = await new GetDocumentTextPreviewQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.Diagnostics())
            .Handle(new(document.Id), CancellationToken.None);

        // Assert
        var extraction = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Equal(TextExtractionStatus.Completed, extraction.Status);
        Assert.NotNull(extraction.Text);
        Assert.Equal("# Notes\n" + new string('a', 1_048_576), extraction.Text);
        Assert.Equal(extraction.Text.Length, extraction.CharacterCount);
        Assert.Null(extraction.LeaseOwner);
        Assert.NotNull(preview);
        Assert.Equal(DocumentTextExtractionOptions.PreviewCharacterLimit, preview.Text.Length);
        Assert.True(preview.IsTruncated);
        Assert.Equal(extraction.CharacterCount, preview.CharacterCount);
        Assert.Equal((document.BlobReference, document.ValidatedETag), Assert.Single(fixture.Storage.Reads));
        Assert.Equal(DocumentStatus.Uploaded, (await db.Documents.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(fixture.Storage.Deletions);
    }

    [Fact]
    public async Task DisableAndReenable_PreservesWorkAndCompletedTextWithoutConsumingRetries()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        // Verify an upload while Markdown extraction is disabled: neither upload
        // finalization nor the reconciliation scheduler should create extraction work.
        fixture.ExtractionOptions.EnabledFormats["markdown"] = false;

        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        var scheduler = fixture.ExtractionScheduler(db);
        await scheduler.ScheduleAsync(CancellationToken.None);
        Assert.Empty(await db.DocumentTextExtractions.ToListAsync());

        // Re-enabling lets the scheduler discover the verified document and stage work.
        fixture.ExtractionOptions.EnabledFormats["markdown"] = true;
        await scheduler.ScheduleAsync(CancellationToken.None);

        // Disable again before a worker claims it; the pending row must remain available.
        fixture.ExtractionOptions.EnabledFormats["markdown"] = false;

        // Act
        // Disabled formats cannot be claimed. The API reports the existing work as Paused.
        var pausedClaim = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        var pausedStatus = await new GetDocumentQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics())
            .Handle(new(document.Id), CancellationToken.None);

        // Resume the same work and complete its first processing attempt.
        fixture.ExtractionOptions.EnabledFormats["markdown"] = true;
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(db).ProcessAsync(work!, CancellationToken.None);

        // Disabling future processing must not hide or downgrade a completed extraction.
        fixture.ExtractionOptions.EnabledFormats["markdown"] = false;
        var completedStatus = await new GetDocumentQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics())
            .Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.Null(pausedClaim);
        Assert.Equal("Paused", pausedStatus!.TextExtraction!.Status);
        // The disabled claim consumed no attempt; only the successful claim incremented it.
        Assert.Equal(1, work!.Attempts);
        Assert.Equal("Completed", completedStatus!.TextExtraction!.Status);
        // Pausing/resuming reused the original work item instead of creating another one.
        Assert.Single(await db.DocumentTextExtractions.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Preview_RemainsBoundedWithTrailingSpacesAndUnicode(bool emojiAtBoundary)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        var prefix = emojiAtBoundary ? new string('a', 3999) + "📝" : "heading";
        var text = prefix + new string(' ', 10_000);

        await fixture.ExtractionQueue(db).CompleteAsync(work!, text, CancellationToken.None);
        var handler = new GetDocumentTextPreviewQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.Diagnostics());

        // Act
        var preview = await handler.Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(emojiAtBoundary ? 3999 : 4000, preview.Text.Length);
        Assert.False(char.IsHighSurrogate(preview.Text[^1]));
        Assert.Equal(text.Length, preview.CharacterCount);
        Assert.True(preview.IsTruncated);
    }

    [Fact]
    public async Task Reconciliation_DoesNotProcessHistoricalUploadsWithoutVerifiedETag()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await ReserveAsync(db);
        document.CompleteUpload("\"legacy\"");
        await db.SaveChangesAsync();

        await db.Documents
            .Where(item => item.Id == document.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ValidatedETag, (string?)null));

        // Act
        await fixture.ExtractionScheduler(db).ScheduleAsync(CancellationToken.None);

        // Assert
        Assert.Empty(await db.DocumentTextExtractions.ToListAsync());
        Assert.Empty(fixture.Storage.Reads);
    }

    [Fact]
    public async Task Scheduler_SchedulesMissingSourcesAcrossOrganizationsAndSkipsExistingSources()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var firstDocument = await VerifyAsync(fixture, db);
        var oldExtraction = await db.DocumentTextExtractions.SingleAsync();
        oldExtraction.SourceETag = "\"older\"";

        var existingDocument = await VerifyAsync(fixture, db);

        var missingDocument = await ReserveAsync(db, organizationId: existingDocument.OrganizationId);
        missingDocument.CompleteUpload(existingDocument.ValidatedETag!);
        await db.SaveChangesAsync();

        // Act
        await fixture.ExtractionScheduler(db).ScheduleAsync(CancellationToken.None);

        // Assert
        var extractions = await db.DocumentTextExtractions.AsNoTracking().ToListAsync();
        Assert.Equal(4, extractions.Count);
        foreach (var document in new[] { firstDocument, existingDocument, missingDocument })
        {
            var extraction = Assert.Single(extractions, item =>
                item.DocumentId == document.Id && item.SourceETag == document.ValidatedETag);
            Assert.Equal(document.OrganizationId, extraction.OrganizationId);
        }
    }

    [Fact]
    public async Task Scheduler_CompetingInsertForSameSourceLeavesOneExtraction()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        var document = await ReserveAsync(setupDb);
        document.CompleteUpload("\"verified\"");
        await setupDb.SaveChangesAsync();
        var interceptor = new BeforeExtractionSave(async cancellationToken =>
        {
            await using var competingDb = fixture.CreateContext();
            fixture.ExtractionScheduler(competingDb).Stage(document);
            await competingDb.SaveChangesAsync(cancellationToken);
        });

        var options = fixture.Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(interceptor).Options;
        
        await using var schedulerDb = new KnowledgeDbContext(options);

        // Act
        await fixture.ExtractionScheduler(schedulerDb).ScheduleAsync(CancellationToken.None);

        // Assert
        var extraction = Assert.Single(await setupDb.DocumentTextExtractions.AsNoTracking().ToListAsync());
        Assert.Equal(document.OrganizationId, extraction.OrganizationId);
        Assert.Equal(document.Id, extraction.DocumentId);
        Assert.Equal(document.ValidatedETag, extraction.SourceETag);
        Assert.Empty(schedulerDb.ChangeTracker.Entries<DocumentTextExtraction>());
    }

    [Fact]
    public async Task Scheduler_InsertFailureWithOnlyOlderSourceRethrowsOriginalException()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        var document = await VerifyAsync(fixture, setupDb);
        var oldExtraction = await setupDb.DocumentTextExtractions.SingleAsync();
        oldExtraction.SourceETag = "\"older\"";
        await setupDb.SaveChangesAsync();

        var failure = new DbUpdateException("Simulated unrelated insert failure.");
        var options = fixture
            .Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(new BeforeExtractionSave(_ => throw failure)).Options;
        await using var schedulerDb = new KnowledgeDbContext(options);

        // Act
        var schedule = () => fixture.ExtractionScheduler(schedulerDb).ScheduleAsync(CancellationToken.None);

        // Assert
        Assert.Same(failure, await Assert.ThrowsAsync<DbUpdateException>(schedule));
        var extraction = Assert.Single(await setupDb.DocumentTextExtractions.AsNoTracking().ToListAsync());
        Assert.NotEqual(document.ValidatedETag, extraction.SourceETag);
        Assert.Empty(schedulerDb.ChangeTracker.Entries<DocumentTextExtraction>());
    }

    [Fact]
    public async Task TwoWorkersCompeting_ReceiveOneActiveLease()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setup = fixture.CreateContext();
        await VerifyAsync(fixture, setup);

        // Act
        var claims = await Task.WhenAll(Task.Run(Claim), Task.Run(Claim));

        // Assert
        Assert.Single(claims, work => work is not null);

        async Task<DocumentTextExtraction?> Claim()
        {
            await using var db = fixture.CreateContext();
            return await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExpiredWorker_CannotPersistOutputOrOverwriteRecoveredResult()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await VerifyAsync(fixture, db);
        var queue = fixture.ExtractionQueue(db);
        var expired = await queue.ClaimAsync(CancellationToken.None);
        fixture.Clock.Advance(fixture.ExtractionWorkerOptions.LeaseSeconds + 1);
        await using var restartedDb = fixture.CreateContext();
        var recovered = await fixture.ExtractionQueue(restartedDb).ClaimAsync(CancellationToken.None);

        // Act
        var expiredCompletion = await queue.CompleteAsync(expired!, "stale", CancellationToken.None);
        await fixture.ExtractionProcessor(restartedDb).ProcessAsync(recovered!, CancellationToken.None);
        var overwrite = await queue.CompleteAsync(expired!, "overwrite", CancellationToken.None);
        var retry = await queue.RetryAsync(expired!, "stale_retry", CancellationToken.None);

        // Assert
        Assert.Equal(0, expiredCompletion);
        Assert.Equal(0, overwrite);
        Assert.Equal(0, retry);
        Assert.NotEqual(expired!.LeaseOwner, recovered!.LeaseOwner);
        var result = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Equal(TextExtractionStatus.Completed, result.Status);
        Assert.Equal(Encoding.UTF8.GetString(fixture.Storage.TextBytes), result.Text);
    }

    [Fact]
    public async Task LeaseExpiryDuringRead_LeavesNoPartialTextAndCanRecover()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        fixture.Storage.BeforeRead = () =>
        {
            fixture.Clock.Advance(fixture.ExtractionWorkerOptions.LeaseSeconds + 1);
            return Task.CompletedTask;
        };

        // Act
        await fixture.ExtractionProcessor(db).ProcessAsync(work!, CancellationToken.None);

        // Assert
        var persisted = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Null(persisted.Text);
        Assert.Null(persisted.CharacterCount);
        Assert.Equal(TextExtractionStatus.Processing, persisted.Status);
        fixture.Storage.BeforeRead = null;
        var recovered = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(db).ProcessAsync(recovered!, CancellationToken.None);
        Assert.Equal(TextExtractionStatus.Completed, (await db.DocumentTextExtractions.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task SourceChangedDuringRead_CannotPublishText()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        fixture.Storage.BeforeRead = async () =>
        {
            await using var changedDb = fixture.CreateContext();
            await changedDb.Documents.Where(item => item.Id == document.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ValidatedETag, "\"different\""));
        };

        // Act
        await fixture.ExtractionProcessor(db).ProcessAsync(work!, CancellationToken.None);

        // Assert
        var persisted = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Null(persisted.Text);
        Assert.Equal(TextExtractionStatus.Failed, persisted.Status);
        Assert.Equal(DocumentTextExtractionErrorCodes.SourceChangedDuringExtraction, persisted.LastError);
    }

    [Fact]
    public async Task MismatchedOrganizationWork_DoesNotReadAnotherOrganizationsBlob()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        work!.OrganizationId = Guid.NewGuid();

        // Act
        var process = () => fixture.ExtractionProcessor(db).ProcessAsync(work, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DocumentTextExtractionException>(process);
        Assert.Equal(DocumentTextExtractionErrorCodes.SourceNotEligible, exception.ErrorCode);
        Assert.Empty(fixture.Storage.Reads);
    }

    [Fact]
    public async Task Persistence_RejectsExtractionWithAnotherOrganizationsDocument()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        db.DocumentTextExtractions.Add(new DocumentTextExtraction
        {
            OrganizationId = Guid.NewGuid(), DocumentId = document.Id, SourceETag = document.ValidatedETag!,
            FormatId = "markdown", ExtractorVersion = "markdown-utf8-v1", CreatedAt = DateTime.UtcNow, NextAttemptAt = DateTime.UtcNow
        });

        // Act
        var save = () => db.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    [Fact]
    public async Task StatusAndPreview_AreScopedToAuthorizedOrganization()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        var work = await fixture.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(db).ProcessAsync(work!, CancellationToken.None);
        var otherOrganization = new ExtractionAccess(Guid.NewGuid());

        // Act
        var preview = await new GetDocumentTextPreviewQueryHandler(db, otherOrganization, fixture.Diagnostics()).Handle(new(document.Id), CancellationToken.None);
        var status = await new GetDocumentQueryHandler(db, otherOrganization, fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics()).Handle(new(document.Id), CancellationToken.None);
        var list = await new GetDocumentsQueryHandler(db, otherOrganization, fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics()).Handle(new(), CancellationToken.None);

        // Assert
        Assert.Null(preview);
        Assert.Null(status);
        Assert.Empty(list);
    }

    [Fact]
    public async Task BasicProjection_OmitsChunkingWithoutQueryingItsTable()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        var document = await VerifyAsync(fixture, setupDb);
        var work = await fixture.ExtractionQueue(setupDb).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(setupDb).ProcessAsync(work!, CancellationToken.None);
        await DocumentChunkingTests.CompleteAsync(fixture, setupDb);
        var commandCounter = new QueryCommandCounter();
        await using var queryDb = new KnowledgeDbContext(fixture.Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(commandCounter).Options);

        // Act
        var projection = await queryDb.Documents.AsNoTracking()
            .Where(item => item.OrganizationId == document.OrganizationId && item.Id == document.Id)
            .SelectDocumentResults(queryDb.DocumentTextExtractions.AsNoTracking())
            .SingleAsync();
        var result = projection.ToResult(fixture.ExtractionStatus());

        // Assert
        Assert.Equal("Completed", result.TextExtraction!.Status);
        Assert.Null(result.Chunking);
        Assert.Equal(1, commandCounter.ReaderCommands);
        Assert.DoesNotContain(commandCounter.CommandTexts, sql => sql.Contains("DocumentChunkings", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetDocument_LoadsDocumentAndCurrentProcessingStatusesWithOneMetadataQuery(bool diagnosticsEnabled)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        var document = await VerifyAsync(fixture, setupDb);
        var work = await fixture.ExtractionQueue(setupDb).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(setupDb).ProcessAsync(work!, CancellationToken.None);
        await DocumentChunkingTests.CompleteAsync(fixture, setupDb);
        var commandCounter = new QueryCommandCounter();
        var options = fixture.Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(commandCounter)
            .Options;
        await using var queryDb = new KnowledgeDbContext(options);
        var handler = new GetDocumentQueryHandler(
            queryDb,
            new ExtractionAccess(document.OrganizationId),
            fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics(diagnosticsEnabled));

        // Act
        var result = await handler.Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.Equal("Completed", result!.TextExtraction!.Status);
        if (diagnosticsEnabled)
        {
            Assert.Equal("Completed", result.Chunking!.Status);
            Assert.Equal(1, result.Chunking.ChunkCount);
        }
        else
        {
            Assert.Null(result.Chunking);
            Assert.DoesNotContain(commandCounter.CommandTexts, sql => sql.Contains("DocumentChunkings", StringComparison.Ordinal));
        }

        Assert.Equal(1, commandCounter.ReaderCommands);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetDocuments_LoadsDocumentsAndCurrentProcessingStatusesWithOneMetadataQuery(bool diagnosticsEnabled)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        var document = await VerifyAsync(fixture, setupDb);
        var work = await fixture.ExtractionQueue(setupDb).ClaimAsync(CancellationToken.None);
        await fixture.ExtractionProcessor(setupDb).ProcessAsync(work!, CancellationToken.None);
        await DocumentChunkingTests.CompleteAsync(fixture, setupDb);
        var commandCounter = new QueryCommandCounter();
        var options = fixture.Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(commandCounter)
            .Options;
        await using var queryDb = new KnowledgeDbContext(options);
        var handler = new GetDocumentsQueryHandler(
            queryDb,
            new ExtractionAccess(document.OrganizationId),
            fixture.ExtractionStatus(), fixture.ChunkingStatus(), fixture.Diagnostics(diagnosticsEnabled));

        // Act
        var results = await handler.Handle(new(), CancellationToken.None);

        // Assert
        var result = Assert.Single(results);
        Assert.Equal("Completed", result.TextExtraction!.Status);
        if (diagnosticsEnabled)
        {
            Assert.Equal("Completed", result.Chunking!.Status);
            Assert.Equal(1, result.Chunking.ChunkCount);
        }
        else
        {
            Assert.Null(result.Chunking);
            Assert.DoesNotContain(commandCounter.CommandTexts, sql => sql.Contains("DocumentChunkings", StringComparison.Ordinal));
        }

        Assert.Equal(1, commandCounter.ReaderCommands);
    }

    [Fact]
    public async Task Worker_RetriesTransientFailureAndCompletesOnceAfterRecovery()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await VerifyAsync(fixture, db);
        fixture.Storage.ReadError = new IOException("Simulated outage.");
        using var services = CreateWorkerServices(fixture);
        var worker = services.GetRequiredService<DocumentTextExtractionWorker>();

        // Act
        await worker.RunBatchAsync(CancellationToken.None);
        var pending = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        fixture.Storage.ReadError = null;
        fixture.Clock.Advance(11);
        await worker.RunBatchAsync(CancellationToken.None);
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TextExtractionStatus.Pending, pending.Status);
        Assert.Equal(DocumentTextExtractionErrorCodes.DependencyFailure, pending.LastError);
        Assert.Null(pending.Text);
        var completed = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Equal(TextExtractionStatus.Completed, completed.Status);
        Assert.Equal(2, completed.Attempts);
        Assert.Equal(2, fixture.Storage.Reads.Count);
    }

    [Theory]
    [InlineData(DocumentTextExtractionErrorCodes.SourceETagChanged)]
    [InlineData(DocumentTextExtractionErrorCodes.SourceMissing)]
    [InlineData(DocumentTextExtractionErrorCodes.SourceSizeLimit)]
    public async Task Worker_PermanentFailurePreservesOriginalAndDoesNotScheduleCleanup(string errorCode)
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await VerifyAsync(fixture, db);
        fixture.Storage.ReadError = new DocumentTextExtractionException(errorCode);
        using var services = CreateWorkerServices(fixture);

        // Act
        await services.GetRequiredService<DocumentTextExtractionWorker>().RunBatchAsync(CancellationToken.None);
        await fixture.Finalizer(db).ProcessAsync(Notification(document), CancellationToken.None);

        // Assert
        var extraction = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Equal(TextExtractionStatus.Failed, extraction.Status);
        Assert.Equal(errorCode, extraction.LastError);
        Assert.Equal(DocumentStatus.Uploaded, (await db.Documents.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await db.UploadInbox.ToListAsync());
        Assert.Empty(fixture.Storage.Deletions);
    }

    [Fact]
    public async Task Worker_ExhaustedRetriesBecomeDeadLetterAndAreNotAutomaticallyRequeued()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await VerifyAsync(fixture, db);
        fixture.Storage.ReadError = new IOException("Simulated outage.");
        using var services = CreateWorkerServices(fixture);
        var worker = services.GetRequiredService<DocumentTextExtractionWorker>();

        // Act
        for (var attempt = 0; attempt < fixture.ExtractionWorkerOptions.MaxAttempts; attempt++)
        {
            await worker.RunBatchAsync(CancellationToken.None);
            fixture.Clock.Advance(600);
        }
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert
        var extraction = await db.DocumentTextExtractions.AsNoTracking().SingleAsync();
        Assert.Equal(TextExtractionStatus.DeadLetter, extraction.Status);
        Assert.Equal(fixture.ExtractionWorkerOptions.MaxAttempts, extraction.Attempts);
        Assert.Equal(fixture.ExtractionWorkerOptions.MaxAttempts, fixture.Storage.Reads.Count);
    }

    internal static async Task<Document> ReserveAsync(KnowledgeDbContext db, string name = "notes.md", Guid? organizationId = null)
    {
        var document = Document.CreatePendingUpload(organizationId ?? Guid.NewGuid(), name, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(15));
        db.Documents.Add(document);
        await db.SaveChangesAsync();
        return document;
    }

    private static async Task<Document> VerifyAsync(UploadTestDatabase fixture, KnowledgeDbContext db)
    {
        var document = await ReserveAsync(db);
        await fixture.Finalizer(db).ProcessAsync(Notification(document), CancellationToken.None);
        return document;
    }

    private static UploadInboxItem Notification(Document document) => new()
    {
        BlobReference = document.BlobReference, Source = "test", EventId = document.Id.ToString("N"), ETag = "\"0x123\""
    };

    private static ServiceProvider CreateWorkerServices(UploadTestDatabase fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(fixture.ExtractionWorkerOptions));
        services.AddScoped(_ => fixture.CreateContext());
        services.AddScoped(provider => fixture.ExtractionScheduler(provider.GetRequiredService<KnowledgeDbContext>()));
        services.AddScoped(provider => fixture.ExtractionQueue(provider.GetRequiredService<KnowledgeDbContext>()));
        services.AddScoped(provider => fixture.ExtractionProcessor(provider.GetRequiredService<KnowledgeDbContext>()));
        services.AddSingleton<DocumentTextExtractionWorker>();
        return services.BuildServiceProvider();
    }

    private sealed class BeforeExtractionSave(Func<CancellationToken, Task> beforeSave) : SaveChangesInterceptor
    {
        private bool invoked;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!invoked && eventData.Context!.ChangeTracker.Entries<DocumentTextExtraction>()
                .Any(entry => entry.State == EntityState.Added))
            {
                invoked = true;
                await beforeSave(cancellationToken);
            }

            return result;
        }
    }

    private sealed class RejectExtractionInsert : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO") && command.CommandText.Contains("DocumentTextExtractions"))
            {
                throw new InvalidOperationException("Simulated extraction insert failure.");
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class QueryCommandCounter : DbCommandInterceptor
    {
        public int ReaderCommands { get; private set; }
        public List<string> CommandTexts { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommands++;
            CommandTexts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

internal sealed class ExtractionAccess(Guid organizationId) : IOrganizationAccessService
{
    public Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IOrganizationMembership>(new ExtractionMembership(organizationId));

    public void ClearCache(Guid userId, Guid organizationId) { }

    private sealed record ExtractionMembership(Guid OrganizationId) : IOrganizationMembership
    {
        public Guid MembershipId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public string Role => "OrganizationOwner";
    }
}
