using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.CompleteDocumentUpload;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents;

public sealed class DocumentHandlersTests
{
    [Fact]
    public async Task BeginDocumentUpload_ReservesOrganizationScopedDocumentAndReturnsBlobUri()
    {
        await using var db = CreateDbContext();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobStorage = new FakeDocumentBlobStorage();
        var handler = CreateBeginHandler(db, organizationId, userId, blobStorage);

        var result = await handler.Handle(
            new BeginDocumentUploadCommand("handbook.pdf"),
            CancellationToken.None);

        var document = await db.Documents.SingleAsync();
        Assert.Equal(result.Document.Id, document.Id);
        Assert.Equal(organizationId, document.OrganizationId);
        Assert.Equal(userId, document.CreatedBy);
        Assert.Equal("handbook.pdf", document.Name);
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.NotNull(document.UploadExpiresAt);
        Assert.Equal(document.BlobReference, Assert.Single(blobStorage.CreatedUploadBlobReferences));
        Assert.Equal("https://storage.example/documents/upload?sig=redacted", result.UploadUri.ToString());
    }

    [Fact]
    public async Task BeginDocumentUpload_WhenSasGenerationFails_DoesNotCreateDocument()
    {
        await using var db = CreateDbContext();
        var blobStorage = new FakeDocumentBlobStorage
        {
            UploadUriException = new InvalidOperationException("Storage is unavailable.")
        };
        var handler = CreateBeginHandler(db, Guid.NewGuid(), Guid.NewGuid(), blobStorage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new BeginDocumentUploadCommand("handbook.pdf"),
            CancellationToken.None).AsTask());

        Assert.Empty(await db.Documents.ToListAsync());
    }

    [Fact]
    public async Task CompleteDocumentUpload_ValidPdf_MarksDocumentUploaded()
    {
        await using var db = CreateDbContext();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobStorage = new FakeDocumentBlobStorage
        {
            UploadInspection = new DocumentUploadInspection(1024, HasPdfSignature: true)
        };
        var beginHandler = CreateBeginHandler(db, organizationId, userId, blobStorage);
        var reservation = await beginHandler.Handle(
            new BeginDocumentUploadCommand("handbook.pdf"),
            CancellationToken.None);
        var completeHandler = CreateCompleteHandler(db, organizationId, userId, blobStorage);

        var result = await completeHandler.Handle(
            new CompleteDocumentUploadCommand(reservation.Document.Id),
            CancellationToken.None);

        var document = await db.Documents.SingleAsync();
        Assert.Equal(DocumentStatus.Uploaded, result.Status);
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.Null(document.UploadExpiresAt);
        Assert.Empty(blobStorage.DeletedBlobReferences);
    }

    [Fact]
    public async Task CompleteDocumentUpload_InvalidBlob_MarksDocumentFailedAndDeletesBlob()
    {
        await using var db = CreateDbContext();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobStorage = new FakeDocumentBlobStorage
        {
            UploadInspection = new DocumentUploadInspection(1024, HasPdfSignature: false)
        };
        var beginHandler = CreateBeginHandler(db, organizationId, userId, blobStorage);
        var reservation = await beginHandler.Handle(
            new BeginDocumentUploadCommand("handbook.pdf"),
            CancellationToken.None);
        var completeHandler = CreateCompleteHandler(db, organizationId, userId, blobStorage);

        await Assert.ThrowsAsync<ArgumentException>(() => completeHandler.Handle(
            new CompleteDocumentUploadCommand(reservation.Document.Id),
            CancellationToken.None).AsTask());

        var document = await db.Documents.SingleAsync();
        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(document.BlobReference, Assert.Single(blobStorage.DeletedBlobReferences));
    }

    [Fact]
    public async Task CompleteDocumentUpload_ExcludesDocumentsBelongingToAnotherOrganization()
    {
        await using var db = CreateDbContext();
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var documentB = Document.CreatePendingUpload(
            organizationBId,
            "organization-b.pdf",
            userId,
            DateTime.UtcNow.AddMinutes(15));
        db.Documents.Add(documentB);
        await db.SaveChangesAsync();
        var handler = CreateCompleteHandler(
            db,
            organizationAId,
            userId,
            new FakeDocumentBlobStorage { UploadInspection = new DocumentUploadInspection(1024, true) });

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new CompleteDocumentUploadCommand(documentB.Id),
            CancellationToken.None).AsTask());

        Assert.Equal(DocumentStatus.PendingForUpload, documentB.Status);
    }

    [Fact]
    public async Task GetDocuments_ExcludesDocumentsBelongingToAnotherOrganization()
    {
        await using var db = CreateDbContext();
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var documentA = Document.CreatePendingUpload(
            organizationAId,
            "organization-a.pdf",
            userId,
            DateTime.UtcNow.AddMinutes(15));
        var documentB = Document.CreatePendingUpload(
            organizationBId,
            "organization-b.pdf",
            userId,
            DateTime.UtcNow.AddMinutes(15));
        db.Documents.AddRange(documentA, documentB);
        await db.SaveChangesAsync();

        var handler = new GetDocumentsQueryHandler(
            db,
            CreateOrganizationAccessService(organizationAId, userId));

        var documents = await handler.Handle(new GetDocumentsQuery(), CancellationToken.None);

        var document = Assert.Single(documents);
        Assert.Equal(documentA.Id, document.Id);
        Assert.NotEqual(documentB.Id, document.Id);
    }

    private static KnowledgeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new KnowledgeDbContext(options);
    }

    private static BeginDocumentUploadCommandHandler CreateBeginHandler(
        KnowledgeDbContext db,
        Guid organizationId,
        Guid userId,
        FakeDocumentBlobStorage blobStorage) => new(
            db,
            CreateOrganizationAccessService(organizationId, userId),
            blobStorage,
            Options.Create(new DocumentUploadOptions()),
            NullLogger<BeginDocumentUploadCommandHandler>.Instance);

    private static CompleteDocumentUploadCommandHandler CreateCompleteHandler(
        KnowledgeDbContext db,
        Guid organizationId,
        Guid userId,
        FakeDocumentBlobStorage blobStorage) => new(
            db,
            CreateOrganizationAccessService(organizationId, userId),
            blobStorage,
            Options.Create(new DocumentUploadOptions()),
            NullLogger<CompleteDocumentUploadCommandHandler>.Instance);

    private static FakeOrganizationAccessService CreateOrganizationAccessService(
        Guid organizationId,
        Guid userId) => new(new FakeOrganizationMembership(organizationId, userId));

    private sealed record FakeOrganizationMembership(Guid OrganizationId, Guid UserId) : IOrganizationMembership
    {
        public Guid MembershipId { get; } = Guid.NewGuid();
        public string Role { get; } = "KnowledgeManager";
    }

    private sealed class FakeDocumentBlobStorage : IDocumentBlobStorage
    {
        public List<string> CreatedUploadBlobReferences { get; } = [];
        public List<string> DeletedBlobReferences { get; } = [];
        public DocumentUploadInspection? UploadInspection { get; init; }
        public Exception? UploadUriException { get; init; }

        public Task<Uri> CreateUploadUriAsync(
            string blobReference,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            CreatedUploadBlobReferences.Add(blobReference);

            if (UploadUriException is not null)
                throw UploadUriException;

            return Task.FromResult(new Uri("https://storage.example/documents/upload?sig=redacted"));
        }

        public Task<DocumentUploadInspection?> InspectUploadAsync(
            string blobReference,
            CancellationToken cancellationToken) => Task.FromResult(UploadInspection);

        public Task DeleteIfExistsAsync(string blobReference, CancellationToken cancellationToken)
        {
            DeletedBlobReferences.Add(blobReference);
            return Task.CompletedTask;
        }
    }
}
