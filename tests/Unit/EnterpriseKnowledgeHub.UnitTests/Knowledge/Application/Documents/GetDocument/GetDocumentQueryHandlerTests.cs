using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.GetDocument;

public sealed class GetDocumentQueryHandlerTests
{
    private const string DocumentName = "handbook.pdf";

    [Theory]
    [InlineData("pdf")]
    [InlineData("md")]
    public async Task GetDocument_WhenDocumentBelongsToAnotherOrganization_ReturnsNull(string extension)
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var otherOrganizationId = Guid.NewGuid();
        var otherDocument = CreatePendingDocument(otherOrganizationId, membership.UserId, $"other.{extension}");

        db.Documents.Add(otherDocument);
        await db.SaveChangesAsync();

        var handler = new GetDocumentQueryHandler(db, new FakeOrganizationAccessService(membership));

        // Act
        var document = await handler.Handle(new GetDocumentQuery(otherDocument.Id), CancellationToken.None);

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
        var handler = new GetDocumentQueryHandler(db, new FakeOrganizationAccessService(membership));

        // Act
        var document = await handler.Handle(new GetDocumentQuery(ownDocument.Id), CancellationToken.None);

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

    private static TestOrganizationMembership CreateMembership() =>
        new(Guid.NewGuid(), Guid.NewGuid());

    private static Document CreatePendingDocument(Guid organizationId, Guid userId, string name) =>
        Document.CreatePendingUpload(
            organizationId,
            name,
            userId,
            DateTime.UtcNow.AddMinutes(15));
}
