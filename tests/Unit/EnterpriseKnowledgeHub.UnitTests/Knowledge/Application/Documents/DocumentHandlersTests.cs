using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;
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
    private const string DocumentName = "handbook.pdf";

    [Fact]
    public async Task BeginDocumentUpload_CommitsOrganizationScopedReservationBeforeReturningUploadUri()
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var blobStorage = new TestDocumentBlobStorage();
        var handler = CreateBeginDocumentUploadHandler(db, membership, blobStorage);

        // Act
        var result = await handler.Handle(
            new BeginDocumentUploadCommand(DocumentName),
            CancellationToken.None);

        // Assert
        db.ChangeTracker.Clear();
        var document = await db.Documents.SingleAsync();

        Assert.Equal(result.Document.Id, document.Id);
        Assert.Equal(membership.OrganizationId, document.OrganizationId);
        Assert.Equal(membership.UserId, document.CreatedBy);
        Assert.Equal(DocumentName, document.Name);
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

    [Fact]
    public async Task GetDocuments_ReturnsOnlyDocumentsFromCurrentOrganization()
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var ownDocument = CreatePendingDocument(membership.OrganizationId, membership.UserId, "own.pdf");
        var otherDocument = CreatePendingDocument(Guid.NewGuid(), membership.UserId, "other.pdf");
        db.Documents.AddRange(ownDocument, otherDocument);
        await db.SaveChangesAsync();
        var handler = new GetDocumentsQueryHandler(
            db,
            new FakeOrganizationAccessService(membership));

        // Act
        var documents = await handler.Handle(new GetDocumentsQuery(), CancellationToken.None);

        // Assert
        var document = Assert.Single(documents);
        Assert.Equal(ownDocument.Id, document.Id);
        Assert.DoesNotContain(documents, item => item.Id == otherDocument.Id);
    }

    [Fact]
    public async Task GetDocument_WhenDocumentBelongsToAnotherOrganization_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var otherDocument = CreatePendingDocument(Guid.NewGuid(), membership.UserId, "other.pdf");
        db.Documents.Add(otherDocument);
        await db.SaveChangesAsync();
        var handler = new GetDocumentQueryHandler(
            db,
            new FakeOrganizationAccessService(membership));

        // Act
        var document = await handler.Handle(
            new GetDocumentQuery(otherDocument.Id),
            CancellationToken.None);

        // Assert
        Assert.Null(document);
    }

    [Fact]
    public async Task GetDocument_WhenDocumentBelongsToCurrentOrganization_ReturnsDocument()
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var ownDocument = CreatePendingDocument(membership.OrganizationId, membership.UserId, DocumentName);
        db.Documents.Add(ownDocument);
        await db.SaveChangesAsync();
        var handler = new GetDocumentQueryHandler(
            db,
            new FakeOrganizationAccessService(membership));

        // Act
        var document = await handler.Handle(
            new GetDocumentQuery(ownDocument.Id),
            CancellationToken.None);

        // Assert
        Assert.NotNull(document);
        Assert.Equal(ownDocument.Id, document.Id);
        Assert.Equal(DocumentName, document.Name);
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
        TestDocumentBlobStorage blobStorage) =>
        new(
            db,
            new FakeOrganizationAccessService(membership),
            blobStorage,
            Options.Create(new DocumentUploadOptions()),
            NullLogger<BeginDocumentUploadCommandHandler>.Instance);

    private static TestOrganizationMembership CreateMembership() =>
        new(Guid.NewGuid(), Guid.NewGuid());

    private static Document CreatePendingDocument(Guid organizationId, Guid userId, string name) =>
        Document.CreatePendingUpload(
            organizationId,
            name,
            userId,
            DateTime.UtcNow.AddMinutes(15));

    private sealed record TestOrganizationMembership(Guid OrganizationId, Guid UserId) : IOrganizationMembership
    {
        public Guid MembershipId { get; } = Guid.NewGuid();
        public string Role => "KnowledgeManager";
    }

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
            CancellationToken cancellationToken,
            string? expectedETag = null) =>
            throw new NotSupportedException();

        public Task DeleteIfExistsAsync(
            string reference,
            string etag,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
