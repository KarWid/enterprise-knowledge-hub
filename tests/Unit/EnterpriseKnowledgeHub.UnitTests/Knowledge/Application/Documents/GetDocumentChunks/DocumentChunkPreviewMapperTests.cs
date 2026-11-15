using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.GetDocumentChunks;

public sealed class DocumentChunkPreviewMapperTests
{
    [Fact]
    public void MapToPreview_PreservesContentAndSourceMetadata()
    {
        // Arrange
        var preview = new ChunkPreviewProjection(Guid.NewGuid(), 2, "A document passage.", "Handbook > Leave", 42);

        // Act
        var result = DocumentChunkPreviewMapper.MapToPreview(preview);

        // Assert
        Assert.Equal(preview.Id, result.Id);
        Assert.Equal(preview.Ordinal, result.Ordinal);
        Assert.Equal(preview.Content, result.Content);
        Assert.Equal(preview.HeadingPath, result.HeadingPath);
        Assert.Equal(preview.TokenCount, result.TokenCount);
        Assert.False(result.IsTruncated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(DocumentChunkPreviewMapper.MaximumPreviewCharacters)]
    public void MapToPreview_ContentWithinLimitIsNotTruncated(int length)
    {
        // Arrange
        var content = new string('a', length);
        var preview = new ChunkPreviewProjection(Guid.NewGuid(), 0, content, null, 100);

        // Act
        var result = DocumentChunkPreviewMapper.MapToPreview(preview);

        // Assert
        Assert.Equal(content, result.Content);
        Assert.Null(result.HeadingPath);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void MapToPreview_OversizedContentIsTruncatedWhileRetainingOriginalTokenCount()
    {
        // Arrange
        var maximum = DocumentChunkPreviewMapper.MaximumPreviewCharacters;
        var preview = new ChunkPreviewProjection(Guid.NewGuid(), 0, new string('a', maximum + 1), null, 1000);

        // Act
        var result = DocumentChunkPreviewMapper.MapToPreview(preview);

        // Assert
        Assert.Equal(new string('a', maximum), result.Content);
        Assert.Equal(preview.TokenCount, result.TokenCount);
        Assert.True(result.IsTruncated);
    }

    [Fact]
    public void MapToPreview_SurrogatePairAcrossBoundaryIsExcludedEntirely()
    {
        // Arrange
        var prefix = new string('a', DocumentChunkPreviewMapper.MaximumPreviewCharacters - 1);
        var preview = new ChunkPreviewProjection(Guid.NewGuid(), 0, prefix + "🙂", null, 1000);

        // Act
        var result = DocumentChunkPreviewMapper.MapToPreview(preview);

        // Assert
        Assert.Equal(prefix, result.Content);
        Assert.True(result.IsTruncated);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("x", true)]
    public void MapToPreview_CompleteSurrogatePairAtBoundaryIsPreserved(string suffix, bool expectedTruncation)
    {
        // Arrange
        var contentAtLimit = new string('a', DocumentChunkPreviewMapper.MaximumPreviewCharacters - 2) + "🙂";
        var preview = new ChunkPreviewProjection(Guid.NewGuid(), 0, contentAtLimit + suffix, null, 1000);

        // Act
        var result = DocumentChunkPreviewMapper.MapToPreview(preview);

        // Assert
        Assert.Equal(contentAtLimit, result.Content);
        Assert.Equal(expectedTruncation, result.IsTruncated);
    }
}
