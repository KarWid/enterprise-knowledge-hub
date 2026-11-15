using System.Text;
using EnterpriseKnowledgeHub.Infrastructure.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using Microsoft.ML.Tokenizers;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class MarkdownChunkingTests
{
    private readonly MarkdownDocumentChunker chunker = new();
    private readonly TiktokenTokenizer tokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");
    private static readonly DocumentChunkingSettings Settings = new(80, 128, 0, 10_000, 5_000_000);

    [Fact]
    public void Headings_KeepRelatedParagraphsTogetherAndSeparateSections()
    {
        // Arrange
        const string text = "# Handbook\n\n## Leave\n\nRequest leave in advance.\n\nAsk your manager.\n\n## Security\n\nLock your computer.";

        // Act
        var chunks = chunker.Chunk(text, "handbook.md", Settings, CancellationToken.None);

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal("Handbook > Leave", chunks[0].HeadingPath);
        Assert.Contains("Request leave in advance.\n\nAsk your manager.", chunks[0].Content);
        Assert.DoesNotContain("Lock your computer", chunks[0].Content);
        Assert.Equal("Handbook > Security", chunks[1].HeadingPath);
        Assert.Contains("Document: handbook.md", chunks[1].Content);
        Assert.Contains("Section: Handbook > Security", chunks[1].Content);
    }

    [Fact]
    public void PlainTextWithoutHeadings_GroupsParagraphsWithoutInventingStructure()
    {
        // Arrange
        const string text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        // Act
        var chunks = chunker.Chunk(text, "notes.MD", Settings, CancellationToken.None);

        // Assert
        var chunk = Assert.Single(chunks);
        Assert.Null(chunk.HeadingPath);
        Assert.EndsWith(text, chunk.Content);
    }

    [Fact]
    public void SetextHeadings_AreRecognizedAndCodeHeadingsRemainContent()
    {
        // Arrange
        const string text = "Handbook\n========\n\nExamples\n--------\n\n```markdown\n# Not a section\n\ncode\n```\n\nFollowing paragraph.";

        // Act
        var chunks = chunker.Chunk(text, "notes.md", Settings, CancellationToken.None);

        // Assert
        var chunk = Assert.Single(chunks);
        Assert.Equal("Handbook > Examples", chunk.HeadingPath);
        Assert.Contains("# Not a section", chunk.Content);
        Assert.Contains("Following paragraph.", chunk.Content);
    }

    [Fact]
    public void SmallListsAndTables_AreKeptIntact()
    {
        // Arrange
        const string text = "- First item\n- Second item\n\n| Name | Value |\n| --- | --- |\n| A | 1 |";

        // Act
        var chunks = chunker.Chunk(text, "notes.md", Settings, CancellationToken.None);

        // Assert
        Assert.EndsWith(text, Assert.Single(chunks).Content);
    }

    [Fact]
    public void OversizedTable_RepeatsHeadersAndRetainsEveryRow()
    {
        // Arrange
        var rows = Enumerable.Range(0, 80).Select(index => $"| item{index} | Some descriptive content about this item | ").ToArray();
        var text = "| Name | Description |\n| --- | --- |\n" + string.Join("\n", rows);

        // Act
        var chunks = chunker.Chunk(text, "table.md", Settings, CancellationToken.None);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.Contains("| Name | Description |\n| --- | --- |", chunk.Content));
        foreach (var row in rows) Assert.Contains(chunks, chunk => chunk.Content.Contains(row.Trim(), StringComparison.Ordinal));
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void OversizedCodeBlock_RepeatsFencesAndDoesNotInventSections()
    {
        // Arrange
        var lines = Enumerable.Range(0, 100).Select(index => $"var value{index} = \"example\";").ToArray();
        var text = "```csharp\n" + string.Join("\n", lines) + "\n```";

        // Act
        var chunks = chunker.Chunk(text, "code.md", Settings, CancellationToken.None);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.Null(chunk.HeadingPath);
            Assert.Contains("```csharp\n", chunk.Content);
            Assert.EndsWith("\n```", chunk.Content);
        });
        foreach (var line in lines) Assert.Contains(chunks, chunk => chunk.Content.Contains(line, StringComparison.Ordinal));
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void LongParagraph_RetainsEveryWordAndProducesDeterministicBoundedChunks()
    {
        // Arrange
        var words = Enumerable.Range(0, 1000).Select(index => $"word{index}").ToArray();
        var text = string.Join(" ", words);

        // Act
        var chunks = chunker.Chunk(text, "plain.md", Settings, CancellationToken.None);
        var repeated = chunker.Chunk(text, "plain.md", Settings, CancellationToken.None);

        // Assert
        Assert.Equal(chunks, repeated);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.Ordinal));
        foreach (var word in words) Assert.Contains(chunks, chunk => chunk.Content.Contains(word, StringComparison.Ordinal));
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void OversizedProse_PrefersCompleteSentencesWhenTheyFit()
    {
        // Arrange
        var sentences = Enumerable.Range(0, 100)
            .Select(index => $"Sentence {index} explains a separate requirement for this document.")
            .ToArray();
        var text = string.Join(" ", sentences);

        // Act
        var chunks = chunker.Chunk(text, "sentences.md", Settings, CancellationToken.None);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.EndsWith(".", chunk.Content));
        foreach (var sentence in sentences)
        {
            Assert.Contains(chunks, chunk => chunk.Content.Contains(sentence, StringComparison.Ordinal));
        }
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void TextWithoutWhitespace_PreservesUnicodeAcrossFallbackBoundaries()
    {
        // Arrange
        var text = string.Concat(Enumerable.Repeat("🙂żółć漢字", 500));

        // Act
        var chunks = chunker.Chunk(text, "unicode.md", Settings, CancellationToken.None);

        // Assert
        var reconstructed = string.Concat(chunks.Select(chunk => chunk.Content[(chunk.Content.IndexOf("\n\n", StringComparison.Ordinal) + 2)..]));
        Assert.Equal(text, reconstructed);
        Assert.All(chunks, chunk => new UTF8Encoding(false, true).GetBytes(chunk.Content));
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void Overlap_RepeatsProseWithinASectionButDoesNotCrossHeadings()
    {
        // Arrange
        var settings = Settings with { TargetTokens = 40, OverlapTokens = 12 };
        var paragraph = string.Join(" ", Enumerable.Repeat("Leave requires approval.", 12));
        var text = "# Leave\n\n" + paragraph + "\n\n" + paragraph + "\n\n# Security\n\nProtect your account.";

        // Act
        var chunks = chunker.Chunk(text, "notes.md", settings, CancellationToken.None);

        // Assert
        var leave = chunks.Where(chunk => chunk.HeadingPath == "Leave").ToArray();
        Assert.True(leave.Length >= 2);
        Assert.True(leave[1].Content.Split("Leave requires approval.").Length > 13);
        var security = Assert.Single(chunks, chunk => chunk.HeadingPath == "Security");
        Assert.DoesNotContain("Leave requires approval.", security.Content);
        AssertTokenBounds(chunks);
    }

    [Fact]
    public void ChunkLimit_RejectsWholeResultInsteadOfReturningPartialContent()
    {
        // Arrange
        const string text = "# One\n\nFirst.\n\n# Two\n\nSecond.";

        // Act
        var generate = () => chunker.Chunk(text, "notes.md", Settings with { MaxChunksPerDocument = 1 }, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentChunkingErrorCodes.ChunkLimitExceeded, Assert.Throws<DocumentChunkingException>(generate).ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n\t")]
    public void EmptyContent_IsRejected(string text)
    {
        // Arrange
        var settings = Settings;

        // Act
        var generate = () => chunker.Chunk(text, "notes.md", settings, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentChunkingErrorCodes.InvalidContent, Assert.Throws<DocumentChunkingException>(generate).ErrorCode);
    }

    [Fact]
    public void SourceLimit_IsEnforcedBeforeParsing()
    {
        // Arrange
        var settings = Settings with { MaxSourceCharacters = 5 };

        // Act
        var generate = () => chunker.Chunk("Longer than the limit.", "notes.md", settings, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentChunkingErrorCodes.SourceLimitExceeded, Assert.Throws<DocumentChunkingException>(generate).ErrorCode);
    }

    [Fact]
    public void Cancellation_StopsChunking()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        var generate = () => chunker.Chunk("Some content.", "notes.md", Settings, cancellation.Token);

        // Assert
        Assert.Throws<OperationCanceledException>(generate);
    }

    private void AssertTokenBounds(IReadOnlyList<DocumentChunkContent> chunks) => Assert.All(chunks, chunk =>
    {
        Assert.InRange(chunk.TokenCount, 1, Settings.MaxTokens);
        Assert.Equal(tokenizer.CountTokens(chunk.Content), chunk.TokenCount);
    });
}
