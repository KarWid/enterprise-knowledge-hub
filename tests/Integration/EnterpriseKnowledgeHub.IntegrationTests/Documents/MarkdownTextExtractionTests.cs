using System.Text;
using EnterpriseKnowledgeHub.Infrastructure.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class MarkdownTextExtractionTests
{
    [Theory]
    [InlineData("\uFEFF# Heading\r\n\r\n- **Item**\r```sql\nSELECT 1;\n```", "# Heading\n\n- **Item**\n```sql\nSELECT 1;\n```")]
    [InlineData("Zażółć gęślą jaźń 📝\ttext", "Zażółć gęślą jaźń 📝\ttext")]
    [InlineData("<script>alert('data only')</script>\n[link](https://example.invalid)", "<script>alert('data only')</script>\n[link](https://example.invalid)")]
    public async Task ExtractAsync_PreservesMarkdownAsDataAndNormalizesLineEndings(string input, string expected)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(input);
        await using var content = new MemoryStream(bytes);
        var extractor = new MarkdownDocumentTextExtractor();

        // Act
        var text = await extractor.ExtractAsync(content, bytes.Length, 10_000, CancellationToken.None);

        // Assert
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(8190)]
    [InlineData(8189)]
    public async Task ExtractAsync_DecodesUnicodeAcrossReadBoundaries(int prefixLength)
    {
        // Arrange
        var expected = new string('a', prefixLength) + "📝\r\nend";
        var bytes = Encoding.UTF8.GetBytes(expected);
        await using var content = new MemoryStream(bytes);

        // Act
        var text = await new MarkdownDocumentTextExtractor().ExtractAsync(content, bytes.Length, 20_000, CancellationToken.None);

        // Assert
        Assert.Equal(expected.Replace("\r\n", "\n"), text);
    }

    public static TheoryData<byte[], string> InvalidContents => new()
    {
        { [], DocumentTextExtractionErrorCodes.EmptyText },
        { Encoding.UTF8.GetBytes("\uFEFF \r\n\t"), DocumentTextExtractionErrorCodes.EmptyText },
        { [(byte)'a', 0xFF], DocumentTextExtractionErrorCodes.InvalidUtf8 },
        { [(byte)'a', 0xC3], DocumentTextExtractionErrorCodes.InvalidUtf8 },
        { Encoding.Unicode.GetBytes("# Wrong encoding"), DocumentTextExtractionErrorCodes.InvalidTextContent },
        { Encoding.UTF8.GetBytes("content\0binary"), DocumentTextExtractionErrorCodes.InvalidTextContent }
    };

    [Theory]
    [MemberData(nameof(InvalidContents))]
    public async Task ExtractAsync_RejectsInvalidContentWithStableCode(byte[] bytes, string expectedCode)
    {
        // Arrange
        await using var content = new MemoryStream(bytes);
        var extractor = new MarkdownDocumentTextExtractor();

        // Act
        var extract = () => extractor.ExtractAsync(content, bytes.Length, 100, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DocumentTextExtractionException>(extract);
        Assert.Equal(expectedCode, exception.ErrorCode);
    }

    [Fact]
    public async Task ExtractAsync_EnforcesNormalizedOutputLimit()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("abc\r\ndef");
        await using var content = new MemoryStream(bytes);

        // Act
        var extract = () => new MarkdownDocumentTextExtractor().ExtractAsync(content, bytes.Length, 6, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DocumentTextExtractionException>(extract);
        Assert.Equal(DocumentTextExtractionErrorCodes.TextLimitExceeded, exception.ErrorCode);
    }

    [Fact]
    public async Task ExtractAsync_AcceptsExactlyTheNormalizedOutputLimitAcrossCrLfBoundary()
    {
        // Arrange
        var expected = new string('a', 8191) + "\n";
        var bytes = Encoding.UTF8.GetBytes(new string('a', 8191) + "\r\n");
        await using var content = new MemoryStream(bytes);

        // Act
        var text = await new MarkdownDocumentTextExtractor().ExtractAsync(content, bytes.Length, expected.Length, CancellationToken.None);

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public async Task ExtractAsync_TruncatedDownloadIsRetryable()
    {
        // Arrange
        await using var content = new MemoryStream("text"u8.ToArray());

        // Act
        var extract = () => new MarkdownDocumentTextExtractor().ExtractAsync(content, 100, 200, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<EndOfStreamException>(extract);
    }

    [Fact]
    public async Task ExtractAsync_ObservesCancellation()
    {
        // Arrange
        await using var content = new MemoryStream("text"u8.ToArray());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        var extract = () => new MarkdownDocumentTextExtractor().ExtractAsync(content, 4, 100, cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(extract);
    }
}
