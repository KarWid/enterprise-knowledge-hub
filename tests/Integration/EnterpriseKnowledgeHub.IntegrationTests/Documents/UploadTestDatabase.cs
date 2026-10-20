using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
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
    public TestDocumentStorage Storage { get; } = new();

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
        Microsoft.Extensions.Options.Options.Create(new DocumentUploadOptions()),
        Microsoft.Extensions.Options.Options.Create(Options), NullLogger<DocumentUploadFinalizer>.Instance);

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

    public Task<Uri> CreateUploadUriAsync(string reference, DateTimeOffset expiry, CancellationToken ct) =>
        Task.FromResult(new Uri("https://teststore.blob.core.windows.net/documents/" + reference + "?sig=test-only"));

    public async Task<DocumentUploadInspection?> InspectUploadAsync(string reference, CancellationToken ct, string? expectedETag = null)
    {
        Inspections++;
        
        if (BeforeInspection is not null) await BeforeInspection();
        if (InspectError is not null) throw InspectError;
        if (Inspection is not null && expectedETag is not null && expectedETag != Inspection.ETag)
            throw new DocumentBlobChangedException();

        return Inspection;
    }

    public Task DeleteIfExistsAsync(string reference, string etag, CancellationToken ct)
    {
        if (FailDeletion) throw new IOException("simulated storage failure");
        Assert.Equal(Inspection!.ETag, etag);
        Deletions.Add((reference, etag));
        Inspection = null;
        return Task.CompletedTask;
    }
}
