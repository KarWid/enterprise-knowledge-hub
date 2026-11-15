using System.Data.Common;
using EnterpriseKnowledgeHub.Infrastructure.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class DocumentChunkingTests
{
    [Fact]
    public async Task FullProjection_SelectsCurrentConfigurationAndPreservesOrganizationIsolation()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await SeedExtractionAsync(db);
        await CompleteAsync(fixture, db);
        await SeedExtractionAsync(db, organizationId: Guid.NewGuid());
        var changed = new DocumentChunkingOptions { TargetTokens = 400 };
        await new DocumentChunkingScheduler(db, fixture.Chunkers, Options.Create(changed),
            Options.Create(fixture.ChunkingWorkerOptions), fixture.Clock).ScheduleAsync(CancellationToken.None);
        var resolver = new DocumentChunkingStatusResolver(fixture.Chunkers, Options.Create(changed),
            Options.Create(fixture.ChunkingWorkerOptions));
        var configurationKeys = resolver.CurrentConfigurationKeys();

        // Act
        var projections = await db.Documents.AsNoTracking()
            .Where(item => item.OrganizationId == document.OrganizationId)
            .SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking(), db.DocumentChunkings.AsNoTracking()
                .Where(item => configurationKeys.Contains(item.ConfigurationKey)))
            .ToListAsync();

        // Assert
        var result = Assert.Single(projections).ToResult(fixture.ExtractionStatus(), resolver);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal("Completed", result.TextExtraction!.Status);
        // The old completed set must not hide the pending work for the new settings.
        Assert.Equal("Pending", result.Chunking!.Status);
        Assert.Null(result.Chunking.ChunkCount);
    }

    [Fact]
    public async Task FullProjection_PreservesDocumentsWithoutExtractionOrChunkingWork()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await DocumentTextExtractionTests.ReserveAsync(db);
        var resolver = fixture.ChunkingStatus();
        var configurationKeys = resolver.CurrentConfigurationKeys();

        // Act
        var projection = await db.Documents.AsNoTracking()
            .Where(item => item.OrganizationId == document.OrganizationId && item.Id == document.Id)
            .SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking(), db.DocumentChunkings.AsNoTracking()
                .Where(item => configurationKeys.Contains(item.ConfigurationKey)))
            .SingleOrDefaultAsync();

        // Assert
        Assert.NotNull(projection);
        var result = projection.ToResult(fixture.ExtractionStatus(), resolver);
        Assert.Equal("NotAvailable", result.TextExtraction!.Status);
        Assert.Equal("NotAvailable", result.Chunking!.Status);
    }

    [Fact]
    public async Task CompletedExtraction_IsChunkedOnceAndRetainsOriginalText()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        const string text = "# Handbook\n\n## Leave\n\nRequest approval.\n\n## Security\n\nLock your computer.";
        var document = await SeedExtractionAsync(db, text);
        using var services = WorkerServices(fixture);

        // Act
        var worker = services.GetRequiredService<DocumentChunkingWorker>();
        await worker.RunBatchAsync(CancellationToken.None);
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert
        var operation = await db.DocumentChunkings.AsNoTracking().SingleAsync();
        Assert.Equal(DocumentChunkingStatus.Completed, operation.Status);
        Assert.Equal(2, operation.ChunkCount);
        Assert.Equal(1, operation.Attempts);
        var chunks = await db.DocumentChunks.AsNoTracking().OrderBy(item => item.Ordinal).ToListAsync();
        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, chunk => Assert.Equal(document.OrganizationId, chunk.OrganizationId));
        Assert.Equal(text, (await db.DocumentTextExtractions.AsNoTracking().SingleAsync()).Text);
        Assert.Equal(DocumentStatus.Uploaded, (await db.Documents.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(fixture.Storage.Reads);
    }

    [Fact]
    public async Task FormatDisabled_PausesClaimsWithoutConsumingAttemptsAndResumesExistingWork()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);
        fixture.ChunkingOptions.EnabledFormats["markdown"] = false;

        // Act
        var paused = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        var attemptsWhilePaused = await db.DocumentChunkings.Select(item => item.Attempts).SingleAsync();
        fixture.ChunkingOptions.EnabledFormats["markdown"] = true;
        var resumed = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        await fixture.ChunkingProcessor(db).ProcessAsync(resumed!, CancellationToken.None);

        // Assert
        Assert.Null(paused);
        Assert.Equal(0, attemptsWhilePaused);
        Assert.Equal(DocumentChunkingStatus.Completed, (await db.DocumentChunkings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task DisabledWorker_DoesNotScheduleOrClaimWork()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        using var services = WorkerServices(fixture, workerOptions: new() { Enabled = false });

        // Act
        await services.GetRequiredService<DocumentChunkingWorker>().RunBatchAsync(CancellationToken.None);

        // Assert
        Assert.Empty(await db.DocumentChunkings.ToListAsync());
    }

    [Fact]
    public async Task Scheduling_BackfillsOnlyCompletedCurrentSourcesAndEnabledFormats()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var current = await SeedExtractionAsync(db);
        var stale = await SeedExtractionAsync(db);
        await SeedExtractionAsync(db, name: "notes.pdf");
        await db.Documents.Where(item => item.Id == stale.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ValidatedETag, "changed"));
        var pending = await DocumentTextExtractionTests.ReserveAsync(db);
        pending.CompleteUpload("verified");
        await db.SaveChangesAsync();

        // Act
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(current.Id, (await db.DocumentChunkings.SingleAsync()).DocumentId);
    }

    [Fact]
    public async Task SettingsChange_CreatesANewSetAndPreviewSelectsTheCurrentConfiguration()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await SeedExtractionAsync(db);
        await CompleteAsync(fixture, db);
        var changed = new DocumentChunkingOptions { TargetTokens = 400 };
        var scheduler = new DocumentChunkingScheduler(db, fixture.Chunkers, Options.Create(changed), Options.Create(fixture.ChunkingWorkerOptions), fixture.Clock);

        // Act
        await scheduler.ScheduleAsync(CancellationToken.None);
        var work = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        await new DocumentChunkingProcessor(db, fixture.ChunkingQueue(db), fixture.Chunkers, Options.Create(changed)).ProcessAsync(work!, CancellationToken.None);
        var status = new DocumentChunkingStatusResolver(fixture.Chunkers, Options.Create(changed), Options.Create(fixture.ChunkingWorkerOptions));
        var preview = await new GetDocumentChunksQueryHandler(db, new ExtractionAccess(document.OrganizationId), status, fixture.Diagnostics())
            .Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.Equal(2, await db.DocumentChunkings.CountAsync());
        var selected = await db.DocumentChunks.Where(item => item.ChunkingId == work!.Id).Select(item => item.Id).ToListAsync();
        Assert.All(preview!.Items, item => Assert.Contains(item.Id, selected));
    }

    [Fact]
    public async Task ExpiredLease_CanBeRecoveredAndOldOwnerCannotPublishChunks()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);
        var original = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        fixture.Clock.Advance(fixture.ChunkingWorkerOptions.LeaseSeconds + 1);
        await using var recoveredDb = fixture.CreateContext();
        var recovered = await fixture.ChunkingQueue(recoveredDb).ClaimAsync(CancellationToken.None);

        // Act
        var staleCompletion = await fixture.ChunkingQueue(db).CompleteAsync(original!, [new(0, "stale", null, 2)], CancellationToken.None);
        await fixture.ChunkingProcessor(recoveredDb).ProcessAsync(recovered!, CancellationToken.None);

        // Assert
        Assert.Equal(0, staleCompletion);
        Assert.NotEqual(original!.LeaseOwner, recovered!.LeaseOwner);
        Assert.Equal(2, recovered.Attempts);
        Assert.DoesNotContain(await db.DocumentChunks.ToListAsync(), chunk => chunk.Content == "stale");
    }

    [Fact]
    public async Task ConcurrentClaims_HaveOnlyOneLeaseOwner()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var firstDb = fixture.CreateContext();
        await using var secondDb = fixture.CreateContext();
        await SeedExtractionAsync(firstDb);
        await fixture.ChunkingScheduler(firstDb).ScheduleAsync(CancellationToken.None);

        // Act
        var claims = await Task.WhenAll(fixture.ChunkingQueue(firstDb).ClaimAsync(CancellationToken.None),
            fixture.ChunkingQueue(secondDb).ClaimAsync(CancellationToken.None));

        // Assert
        Assert.Single(claims, claim => claim is not null);
    }

    [Fact]
    public async Task ChangedSource_PreventsCompletionAndPersistsNoChunks()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await SeedExtractionAsync(db);
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);
        var work = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        await db.Documents.Where(item => item.Id == document.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ValidatedETag, "changed"));

        // Act
        var completed = await fixture.ChunkingQueue(db).CompleteAsync(work!, [new(0, "text", null, 1)], CancellationToken.None);

        // Assert
        Assert.Equal(0, completed);
        Assert.Empty(await db.DocumentChunks.ToListAsync());
    }

    [Fact]
    public async Task IncompleteExtraction_PreventsChunkSetPublication()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);
        var work = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        await db.DocumentTextExtractions.Where(extraction => extraction.Id == work!.ExtractionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(extraction => extraction.Status, TextExtractionStatus.Failed));

        // Act
        var completed = await fixture.ChunkingQueue(db)
            .CompleteAsync(work!, [new(0, "content", null, 1)], CancellationToken.None);

        // Assert
        Assert.Equal(0, completed);
        Assert.Empty(await db.DocumentChunks.ToListAsync());
        Assert.Equal(DocumentChunkingStatus.Processing, (await db.DocumentChunkings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Preview_ChangedDocumentSourceHidesPreviouslyCompletedChunks()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await SeedExtractionAsync(db);
        await CompleteAsync(fixture, db);
        await db.Documents.Where(item => item.Id == document.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ValidatedETag, "changed-source"));
        var handler = new GetDocumentChunksQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.ChunkingStatus(), fixture.Diagnostics());

        // Act
        var preview = await handler.Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.Null(preview);
        Assert.NotEmpty(await db.DocumentChunks.ToListAsync());
    }

    [Fact]
    public async Task ChunkInsertFailure_RollsBackCompletionAndAllChunkRows()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var setupDb = fixture.CreateContext();
        await SeedExtractionAsync(setupDb);
        await fixture.ChunkingScheduler(setupDb).ScheduleAsync(CancellationToken.None);
        var work = await fixture.ChunkingQueue(setupDb).ClaimAsync(CancellationToken.None);
        await using var failingDb = new KnowledgeDbContext(fixture.Configure(new DbContextOptionsBuilder<KnowledgeDbContext>())
            .AddInterceptors(new RejectChunkInsert()).Options);

        // Act
        var complete = () => fixture.ChunkingQueue(failingDb).CompleteAsync(work!, [new(0, "text", null, 1)], CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(complete);
        Assert.Empty(await setupDb.DocumentChunks.ToListAsync());
        Assert.Equal(DocumentChunkingStatus.Processing, (await setupDb.DocumentChunkings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task OrganizationOwnership_IsEnforcedByDatabaseForeignKeys()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        await CompleteAsync(fixture, db);
        var operation = await db.DocumentChunkings.AsNoTracking().SingleAsync();
        db.DocumentChunks.Add(new() { OrganizationId = Guid.NewGuid(), DocumentId = operation.DocumentId,
            ChunkingId = operation.Id, Ordinal = 100, Content = "forged", TokenCount = 2 });

        // Act
        var save = () => db.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    [Fact]
    public async Task Preview_IsOrganizationScopedOrderedAndPaged()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var document = await SeedExtractionAsync(db, "# One\n\nFirst.\n\n# Two\n\nSecond.");
        await CompleteAsync(fixture, db);
        var own = new GetDocumentChunksQueryHandler(db, new ExtractionAccess(document.OrganizationId), fixture.ChunkingStatus(), fixture.Diagnostics());
        var other = new GetDocumentChunksQueryHandler(db, new ExtractionAccess(Guid.NewGuid()), fixture.ChunkingStatus(), fixture.Diagnostics());

        // Act
        var first = await own.Handle(new(document.Id, limit: 1), CancellationToken.None);
        var second = await own.Handle(new(document.Id, offset: 1, limit: 1), CancellationToken.None);
        var denied = await other.Handle(new(document.Id), CancellationToken.None);

        // Assert
        Assert.Equal(0, Assert.Single(first!.Items).Ordinal);
        Assert.True(first.HasMore);
        Assert.Equal(1, Assert.Single(second!.Items).Ordinal);
        Assert.False(second.HasMore);
        Assert.Null(denied);
    }

    [Fact]
    public async Task Worker_RetriesTransientFailureThenCompletesWithoutDuplicateChunks()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        var chunker = new FailingChunker { FailuresRemaining = 1 };
        using var services = WorkerServices(fixture, chunker);
        var worker = services.GetRequiredService<DocumentChunkingWorker>();

        // Act
        await worker.RunBatchAsync(CancellationToken.None);
        var pending = await db.DocumentChunkings.AsNoTracking().SingleAsync();
        fixture.Clock.Advance(600);
        await worker.RunBatchAsync(CancellationToken.None);

        // Assert
        Assert.Equal(DocumentChunkingStatus.Pending, pending.Status);
        Assert.Empty(await db.DocumentChunks.Where(chunk => chunk.Content == "partial").ToListAsync());
        var completed = await db.DocumentChunkings.AsNoTracking().SingleAsync();
        Assert.Equal(DocumentChunkingStatus.Completed, completed.Status);
        Assert.Equal(2, completed.Attempts);
        Assert.Single(await db.DocumentChunks.ToListAsync());
    }

    [Fact]
    public async Task Worker_ExhaustedRetriesBecomeDeadLetterAndAreNotRescheduled()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        await SeedExtractionAsync(db);
        using var services = WorkerServices(fixture, new FailingChunker { FailuresRemaining = 100 });
        var worker = services.GetRequiredService<DocumentChunkingWorker>();

        // Act
        for (var attempt = 0; attempt <= fixture.ChunkingWorkerOptions.MaxAttempts; attempt++)
        {
            await worker.RunBatchAsync(CancellationToken.None);
            fixture.Clock.Advance(600);
        }

        // Assert
        var operation = await db.DocumentChunkings.AsNoTracking().SingleAsync();
        Assert.Equal(DocumentChunkingStatus.DeadLetter, operation.Status);
        Assert.Equal(fixture.ChunkingWorkerOptions.MaxAttempts, operation.Attempts);
        Assert.Empty(await db.DocumentChunks.ToListAsync());
    }

    internal static async Task<Document> SeedExtractionAsync(KnowledgeDbContext db, string text = "Plain document content.", string name = "notes.md", Guid? organizationId = null)
    {
        var document = await DocumentTextExtractionTests.ReserveAsync(db, name, organizationId);
        document.CompleteUpload("verified-source");
        db.DocumentTextExtractions.Add(new()
        {
            OrganizationId = document.OrganizationId, DocumentId = document.Id, SourceETag = document.ValidatedETag!,
            FormatId = DocumentFormat.FromFileName(name)!.Id, ExtractorVersion = "test-v1", Status = TextExtractionStatus.Completed,
            Text = text, CharacterCount = text.Length, CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return document;
    }

    internal static async Task CompleteAsync(UploadTestDatabase fixture, KnowledgeDbContext db)
    {
        await fixture.ChunkingScheduler(db).ScheduleAsync(CancellationToken.None);
        var work = await fixture.ChunkingQueue(db).ClaimAsync(CancellationToken.None);
        await fixture.ChunkingProcessor(db).ProcessAsync(work!, CancellationToken.None);
    }

    private static ServiceProvider WorkerServices(UploadTestDatabase fixture, IDocumentChunker? chunker = null, DocumentChunkingWorkerOptions? workerOptions = null)
    {
        var services = new ServiceCollection();
        var registry = chunker is null ? fixture.Chunkers : new DocumentChunkerRegistry([chunker]);
        services.AddLogging();
        services.AddSingleton(Options.Create(workerOptions ?? fixture.ChunkingWorkerOptions));
        services.AddScoped(_ => fixture.CreateContext());
        services.AddScoped<IDocumentChunkingScheduler>(provider => new DocumentChunkingScheduler(provider.GetRequiredService<KnowledgeDbContext>(),
            registry, Options.Create(fixture.ChunkingOptions), Options.Create(fixture.ChunkingWorkerOptions), fixture.Clock));
        services.AddScoped(provider => fixture.ChunkingQueue(provider.GetRequiredService<KnowledgeDbContext>()));
        services.AddScoped<IDocumentChunkingProcessor>(provider => new DocumentChunkingProcessor(provider.GetRequiredService<KnowledgeDbContext>(),
            provider.GetRequiredService<IDocumentChunkingQueue>(), registry, Options.Create(fixture.ChunkingOptions)));
        services.AddSingleton<DocumentChunkingWorker>();
        return services.BuildServiceProvider();
    }

    private sealed class FailingChunker : IDocumentChunker
    {
        private readonly MarkdownDocumentChunker inner = new();
        public int FailuresRemaining { get; set; }
        public DocumentFormat Format => inner.Format;
        public string Version => inner.Version;
        public IReadOnlyList<DocumentChunkContent> Chunk(string text, string name, DocumentChunkingSettings settings, CancellationToken cancellationToken)
        {
            if (FailuresRemaining-- > 0) throw new IOException("Simulated temporary failure.");
            return inner.Chunk(text, name, settings, cancellationToken);
        }
    }

    private sealed class RejectChunkInsert : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO") && (command.CommandText.Contains("\"DocumentChunks\"") || command.CommandText.Contains("[DocumentChunks]")))
                throw new InvalidOperationException("Simulated chunk insert failure.");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
