using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.GetDocumentChunks;

public sealed class GetDocumentChunksQueryTests
{
    [Theory]
    [InlineData(-1, 5, "offset")]
    [InlineData(0, 0, "limit")]
    [InlineData(0, -1, "limit")]
    [InlineData(0, 21, "limit")]
    public void InvalidPagination_IsRejectedWhenQueryIsCreated(int offset, int limit, string parameterName)
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var createQuery = () => new GetDocumentChunksQuery(documentId, offset, limit);

        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(createQuery);
        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(int.MaxValue, 20)]
    public void BoundaryPaginationValues_AreAccepted(int offset, int limit)
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var query = new GetDocumentChunksQuery(documentId, offset, limit);

        // Assert
        Assert.Equal(documentId, query.DocumentId);
        Assert.Equal(offset, query.Offset);
        Assert.Equal(limit, query.Limit);
    }
}
