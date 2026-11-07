using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.GetDocuments;

public sealed class GetDocumentsQueryHandlerTests
{
    [Theory]
    [InlineData("pdf")]
    [InlineData("md")]
    public async Task GetDocuments_ReturnsOnlyDocumentsFromCurrentOrganization(string extension)
    {
        // Arrange
        await using var db = CreateDbContext();
        var membership = CreateMembership();
        var ownDocument = CreatePendingDocument(membership.OrganizationId, membership.UserId, $"own.{extension}");

        var anotherOrganizationId = Guid.NewGuid();
        var otherDocument = CreatePendingDocument(anotherOrganizationId, membership.UserId, $"other.{extension}");
        db.Documents.AddRange(ownDocument, otherDocument);
        await db.SaveChangesAsync();

        var handler = new GetDocumentsQueryHandler(db, new FakeOrganizationAccessService(membership));

        // Act
        var documents = await handler.Handle(new GetDocumentsQuery(), CancellationToken.None);

        // Assert
        var document = Assert.Single(documents);
        Assert.Equal(ownDocument.Id, document.Id);
        Assert.DoesNotContain(documents, item => item.Id == otherDocument.Id);
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
