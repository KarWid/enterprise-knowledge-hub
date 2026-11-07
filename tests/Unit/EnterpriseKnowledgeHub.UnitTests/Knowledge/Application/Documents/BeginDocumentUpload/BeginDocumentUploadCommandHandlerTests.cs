using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.BeginDocumentUpload;

public sealed class BeginDocumentUploadCommandHandlerTests
{
    private const string DocumentName = "handbook.pdf";

    [Theory]
    [InlineData("handbook.pdf", "application/pdf", ".pdf")]
    [InlineData("handbook.MD", "text/markdown", ".md")]
    public async Task BeginDocumentUpload_CommitsOrganizationScopedReservationBeforeReturningUploadUri(
        string fileName, string contentType, string extension)
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var blobStorage = new TestDocumentBlobStorage();
        var handler = CreateBeginDocumentUploadHandler(db, membership, blobStorage);

        // Act
        var result = await handler.Handle(
            new BeginDocumentUploadCommand(fileName),
            CancellationToken.None);

        // Assert
        db.ChangeTracker.Clear();
        var document = await db.Documents.SingleAsync();

        Assert.Equal(result.Document.Id, document.Id);
        Assert.Equal(membership.OrganizationId, document.OrganizationId);
        Assert.Equal(membership.UserId, document.CreatedBy);
        Assert.Equal(fileName, document.Name);
        Assert.Equal(contentType, result.Document.ContentType);
        Assert.EndsWith(extension, document.BlobReference);
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.NotNull(document.UploadExpiresAt);
        Assert.Contains(document.BlobReference, result.UploadUri.ToString());
        Assert.Equal(document.BlobReference, Assert.Single(blobStorage.UploadUriRequests));
    }

    [Fact]
    public async Task BeginDocumentUpload_WhenSasGenerationFails_DoesNotPersistReservation()
    {
        // Arrange
        await using var db = CreateDbContext();
        var blobStorage = new TestDocumentBlobStorage { FailUploadUriCreation = true };
        var handler = CreateBeginDocumentUploadHandler(db, CreateMembership(), blobStorage);

        // Act
        Task BeginUpload() => handler
            .Handle(new BeginDocumentUploadCommand(DocumentName), CancellationToken.None)
            .AsTask();

        // Assert
        await Assert.ThrowsAsync<IOException>(BeginUpload);
        Assert.Empty(await db.Documents.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData("notes.md")]
    [InlineData("notes.MD")]
    [InlineData("notes.docx")]
    [InlineData("notes.md.exe")]
    public async Task DisabledOrUnsupportedFormat_DoesNotCreateReservationOrSas(string fileName)
    {
        // Arrange
        await using var db = CreateDbContext();
        var storage = new TestDocumentBlobStorage();
        var settings = new DocumentUploadOptions();
        settings.EnabledFormats["markdown"] = false;
        var handler = CreateBeginDocumentUploadHandler(db, CreateMembership(), storage, settings);

        // Act
        Task BeginUpload() => handler.Handle(
            new BeginDocumentUploadCommand(fileName), CancellationToken.None).AsTask();

        // Assert
        await Assert.ThrowsAsync<DomainException>(BeginUpload);

        Assert.Empty(storage.UploadUriRequests);
        Assert.Empty(await db.Documents.ToListAsync());
    }

    private static KnowledgeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new KnowledgeDbContext(options);
    }

    private static BeginDocumentUploadCommandHandler CreateBeginDocumentUploadHandler(
        KnowledgeDbContext db,
        TestOrganizationMembership membership,
        TestDocumentBlobStorage blobStorage,
        DocumentUploadOptions? settings = null) =>
        new(
            db,
            new FakeOrganizationAccessService(membership),
            blobStorage,
            Options.Create(settings ?? new DocumentUploadOptions()),
            NullLogger<BeginDocumentUploadCommandHandler>.Instance);

    private static TestOrganizationMembership CreateMembership() =>
        new(Guid.NewGuid(), Guid.NewGuid());

    private sealed class TestDocumentBlobStorage : IDocumentBlobStorage
    {
        public bool FailUploadUriCreation { get; init; }
        public List<string> UploadUriRequests { get; } = [];

        public Task<Uri> CreateUploadUriAsync(
            string reference,
            DateTimeOffset expiry,
            CancellationToken cancellationToken)
        {
            UploadUriRequests.Add(reference);

            if (FailUploadUriCreation)
            {
                throw new IOException("Simulated failure while creating the upload URI.");
            }

            return Task.FromResult(
                new Uri($"https://storage.example/documents/{reference}?sig=redacted"));
        }

        public Task<DocumentUploadInspection?> InspectUploadAsync(
            string reference,
            long maximumSizeBytes,
            CancellationToken cancellationToken,
            string? expectedETag = null) =>
            throw new NotSupportedException();

        public Task<string?> GetUploadETagAsync(string reference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteIfExistsAsync(
            string reference,
            string etag,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
