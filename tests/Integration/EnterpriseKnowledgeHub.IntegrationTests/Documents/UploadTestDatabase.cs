using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Infrastructure.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

internal sealed class UploadTestDatabase : IDisposable
{
    private readonly SqliteConnection? keeper;
    public bool UseSqlServer { get; } = Environment.GetEnvironmentVariable("EKH_TEST_SQLSERVER") == "1";
    public string ConnectionString { get; }
    public UploadClock Clock { get; } = new();
    public DocumentUploadWorkerOptions Options { get; } = new() { MaxAttempts = 3 };
    public DocumentUploadOptions UploadOptions { get; } = new();
    public TestDocumentStorage Storage { get; } = new();
    public DocumentTextExtractionOptions ExtractionOptions { get; } = new();
    public DocumentTextExtractionWorkerOptions ExtractionWorkerOptions { get; } = new() { MaxAttempts = 3 };
    public DocumentTextExtractorRegistry Extractors { get; } = new([new MarkdownDocumentTextExtractor()]);

    public UploadTestDatabase()
    {
        ConnectionString = UseSqlServer
            ? $"Server=(localdb)\\MSSQLLocalDB;Database=EKHUploadTests_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true"
            : $"Data Source=uploads-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=10";
        
        if (!UseSqlServer)
        {
            keeper = new SqliteConnection(ConnectionString);
            keeper.Open();
        }

        using var db = CreateContext();
        try
        {
            if (UseSqlServer) db.Database.Migrate();
            else db.Database.EnsureCreated();
        }
        catch
        {
            if (UseSqlServer)
            {
                db.Database.EnsureDeleted();  
            }
            keeper?.Dispose();
            throw;
        }
    }

    public KnowledgeDbContext CreateContext() => new(Configure(new DbContextOptionsBuilder<KnowledgeDbContext>()).Options);

    public DbContextOptionsBuilder<KnowledgeDbContext> Configure(DbContextOptionsBuilder<KnowledgeDbContext> builder) =>
        UseSqlServer ? builder.UseSqlServer(ConnectionString) : builder.UseSqlite(ConnectionString);

    public IUploadInbox Inbox(KnowledgeDbContext db) =>
        new UploadInbox(db, Clock, Microsoft.Extensions.Options.Options.Create(Options));

    public IDocumentUploadFinalizer Finalizer(KnowledgeDbContext db) => new DocumentUploadFinalizer(db, Storage, Inbox(db), Clock,
        Microsoft.Extensions.Options.Options.Create(UploadOptions),
        Microsoft.Extensions.Options.Options.Create(Options), NullLogger<DocumentUploadFinalizer>.Instance, ExtractionScheduler(db));

    public IDocumentTextExtractionScheduler ExtractionScheduler(KnowledgeDbContext db) => new DocumentTextExtractionScheduler(
        db, Extractors, Microsoft.Extensions.Options.Options.Create(ExtractionOptions),
        Microsoft.Extensions.Options.Options.Create(ExtractionWorkerOptions), Clock);

    public IDocumentTextExtractionQueue ExtractionQueue(KnowledgeDbContext db) => new DocumentTextExtractionQueue(
        db, Clock, Microsoft.Extensions.Options.Options.Create(ExtractionOptions), Microsoft.Extensions.Options.Options.Create(ExtractionWorkerOptions));

    public IDocumentTextExtractionProcessor ExtractionProcessor(KnowledgeDbContext db) => new DocumentTextExtractionProcessor(
        db, Storage, ExtractionQueue(db), Extractors, Microsoft.Extensions.Options.Options.Create(ExtractionOptions));

    public DocumentTextExtractionStatusResolver ExtractionStatus() => new(
        Microsoft.Extensions.Options.Options.Create(ExtractionOptions), Microsoft.Extensions.Options.Options.Create(ExtractionWorkerOptions));

    public void Dispose()
    {
        keeper?.Dispose();
        if (UseSqlServer)
        {
            using var db = CreateContext();
            db.Database.EnsureDeleted(); // Only the unique local test database created by this fixture.
        }
    }
}

internal sealed class UploadClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(int seconds) => Now = Now.AddSeconds(seconds);
}

internal sealed class TestDocumentStorage : IDocumentBlobStorage
{
    public DocumentUploadInspection? Inspection { get; set; } = new(100, true, "\"0x123\"");
    public Exception? InspectError { get; set; }
    public bool FailDeletion { get; set; }
    public int Inspections { get; private set; }
    public List<(string Reference, string ETag)> Deletions { get; } = [];
    public Func<Task>? BeforeInspection { get; set; }
    public byte[] TextBytes { get; set; } = System.Text.Encoding.UTF8.GetBytes("# Test document\nKnowledge content.");
    public Exception? ReadError { get; set; }
    public List<(string Reference, string ETag)> Reads { get; } = [];
    public Func<Task>? BeforeRead { get; set; }

    public async Task<DocumentTextSource> OpenVerifiedReadAsync(string reference, string expectedETag, long maximumSizeBytes, CancellationToken ct)
    {
        Reads.Add((reference, expectedETag));
        if (BeforeRead is not null)
        {
            await BeforeRead();
        }
        if (ReadError is not null)
        {
            throw ReadError;
        }
        if (expectedETag != Inspection?.ETag)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.SourceETagChanged);
        }
        if (TextBytes.Length > maximumSizeBytes)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.SourceSizeLimit);
        }
        return new(new MemoryStream(TextBytes), TextBytes.Length);
    }

    public Task<Uri> CreateUploadUriAsync(string reference, DateTimeOffset expiry, CancellationToken ct) =>
        Task.FromResult(new Uri("https://teststore.blob.core.windows.net/documents/" + reference + "?sig=test-only"));

    public async Task<DocumentUploadInspection?> InspectUploadAsync(string reference, long maximumSizeBytes, CancellationToken ct, string? expectedETag = null)
    {
        Inspections++;
        
        if (BeforeInspection is not null) await BeforeInspection();
        if (InspectError is not null) throw InspectError;
        if (Inspection is not null && expectedETag is not null && expectedETag != Inspection.ETag)
            throw new DocumentBlobChangedException();

        return Inspection;
    }

    public async Task<string?> GetUploadETagAsync(string reference, CancellationToken ct) =>
        (await InspectUploadAsync(reference, long.MaxValue, ct))?.ETag;

    public Task DeleteIfExistsAsync(string reference, string etag, CancellationToken ct)
    {
        if (FailDeletion) throw new IOException("simulated storage failure");
        Assert.Equal(Inspection!.ETag, etag);
        Deletions.Add((reference, etag));
        Inspection = null;
        return Task.CompletedTask;
    }
}
