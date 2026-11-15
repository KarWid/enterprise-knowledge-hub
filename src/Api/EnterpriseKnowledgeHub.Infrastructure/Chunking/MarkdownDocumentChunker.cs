using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Microsoft.ML.Tokenizers;

namespace EnterpriseKnowledgeHub.Infrastructure.Chunking;

internal sealed class MarkdownDocumentChunker : IDocumentChunker
{
    private const int FormattingTokenAllowance = 8;
    private const int MaximumHeadingTitleCharacters = 256;
    private const int ContextTokenBudgetDivisor = 3;

    // Vocabulary is packaged locally: chunking never downloads models or fetches document links.
    private readonly TiktokenTokenizer tokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
    public DocumentFormat Format => DocumentFormat.Markdown;
    public string Version => "markdown-structure-cl100k-v1";

    /// <summary>
    /// Splits extracted Markdown into ordered chunks, keeping related blocks within the same section together.
    /// </summary>
    /// <param name="text">The full extracted Markdown, including its original syntax and line breaks.</param>
    /// <param name="documentName">The source document name to include in each chunk's context label.</param>
    /// <param name="settings">The target size, hard token limit, overlap, and document processing bounds.</param>
    /// <param name="cancellationToken">Stops processing between blocks and generated pieces.</param>
    /// <returns>The complete chunk set with zero-based ordinals, heading paths, and token counts.</returns>
    /// <remarks>
    /// Headings start new sections. Plain text works without headings. A block may exceed the target
    /// size when keeping it intact is possible within the hard limit. All per-document state stays
    /// local to this call, allowing the chunker instance to be shared across documents.
    /// </remarks>
    public IReadOnlyList<DocumentChunkContent> Chunk(
        string text,
        string documentName,
        DocumentChunkingSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateChunkInput(text, settings);

        // Parsing identifies structure; it does not render HTML or fetch linked resources.
        var parsed = Markdown.Parse(text, pipeline);
        var result = new List<DocumentChunkContent>();
        var headings = new List<(int Level, string Title)>();
        var buffer = new List<Piece>();
        string? headingPath = null;
        var prefix = ContextPrefix(documentName, headingPath, settings);

        foreach (var block in parsed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read the block from its source span so lists, code fences, and table syntax are retained.
            var raw = text.Substring(block.Span.Start, block.Span.Length).Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            if (block is HeadingBlock heading)
            {
                // Flush before changing context so the previous section keeps its own heading path.
                EmitChunk(buffer, result, prefix, headingPath, settings);

                // A new heading replaces siblings and their descendants, while preserving ancestors.
                headings.RemoveAll(item => item.Level >= heading.Level);
                
                var title = Markdown.ToPlainText(raw, pipeline).Trim();
                headings.Add((heading.Level, LimitCharacters(title, MaximumHeadingTitleCharacters)));
                headingPath = string.Join(" > ", headings.Select(item => item.Title));
                prefix = ContextPrefix(documentName, headingPath, settings);
                continue;
            }

            // Reserve room for the context label and formatting. EmitChunk checks the final count again.
            var payloadBudget = settings.MaxTokens - tokenizer.CountTokens(prefix) - FormattingTokenAllowance;
            foreach (var piece in SplitBlock(block, raw, text, payloadBudget, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = prefix + string.Join("\n\n", buffer.Select(item => item.Text).Append(piece.Text));
                var exceedsTargetSize = tokenizer.CountTokens(candidate) > settings.TargetTokens;
                if (buffer.Count > 0 && exceedsTargetSize)
                {
                    // Only prose can overlap. Copying fragments of code or tables could break their structure.
                    var overlap = string.Empty;
                    if (buffer[^1].Prose && piece.Prose)
                    {
                        overlap = Overlap(buffer[^1].Text, settings.OverlapTokens);
                    }

                    EmitChunk(buffer, result, prefix, headingPath, settings);

                    if (overlap.Length > 0)
                    {
                        var contentWithOverlap = prefix + overlap + "\n\n" + piece.Text;
                        var overlapFits = tokenizer.CountTokens(contentWithOverlap) <= settings.MaxTokens;
                        if (overlapFits)
                        {
                            buffer.Add(new Piece(overlap, true));
                        }
                    }
                }
                buffer.Add(piece);
            }
        }

        EmitChunk(buffer, result, prefix, headingPath, settings);

        // Heading-only files and source constructs without content blocks still produce bounded text.
        if (result.Count == 0)
        {
            var payloadBudget = settings.MaxTokens - tokenizer.CountTokens(prefix) - FormattingTokenAllowance;
            foreach (var piece in SplitText(text, payloadBudget, preferLines: false, cancellationToken))
            {
                buffer.Add(new(piece, false));
                EmitChunk(buffer, result, prefix, headingPath, settings);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }

    private void EmitChunk(
        List<Piece> buffer,
        List<DocumentChunkContent> result,
        string prefix,
        string? headingPath,
        DocumentChunkingSettings settings)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var content = prefix + string.Join("\n\n", buffer.Select(piece => piece.Text));
        var count = tokenizer.CountTokens(content);
        if (count > settings.MaxTokens)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.InvalidChunks);
        }

        if (result.Count >= settings.MaxChunksPerDocument)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.ChunkLimitExceeded);
        }

        result.Add(new DocumentChunkContent(result.Count, content, headingPath, count));
        buffer.Clear();
    }

    private void ValidateChunkInput(string text, DocumentChunkingSettings settings)
    {
        if (!settings.IsValid())
        {
            throw new ArgumentException("Invalid chunking settings.", nameof(settings));
        }

        if (text.Length > settings.MaxSourceCharacters)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceLimitExceeded);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.InvalidContent);
        }
    }

    /// <summary>
    /// Keeps a Markdown block intact when it fits, otherwise splits it using its structural boundaries.
    /// </summary>
    /// <param name="block">The parsed block that identifies a paragraph, list, fenced code block, or table.</param>
    /// <param name="raw">The block's original Markdown text.</param>
    /// <param name="source">The full document, used to read the source spans of individual list items.</param>
    /// <param name="budget">The token allowance for block content, after reserving the shared context prefix.</param>
    /// <param name="cancellationToken">Stops the underlying text splitting between pieces.</param>
    /// <returns>Content pieces marked as prose only when they originate from paragraphs.</returns>
    /// <remarks>
    /// Oversized lists split by item, code fragments repeat their fences, and table fragments repeat
    /// their column headers. If a structural wrapper itself takes too much space, bounded text
    /// splitting is the fallback. This method does not add the document/section context prefix.
    /// </remarks>
    private IEnumerable<Piece> SplitBlock(Block block, string raw, string source, int budget, CancellationToken cancellationToken)
    {
        if (tokenizer.CountTokens(raw) <= budget)
        {
            yield return new(raw, block is ParagraphBlock);
            yield break;
        }

        if (block is ListBlock list)
        {
            // Prefer complete list items over cutting arbitrarily through the whole list.
            foreach (var item in list)
            {
                var itemText = source.Substring(item.Span.Start, item.Span.Length).Trim();
                foreach (var part in SplitText(itemText, budget, true, cancellationToken))
                {
                    yield return new(part, false);
                }
            }

            yield break;
        }

        if (block is FencedCodeBlock)
        {
            var lines = raw.Split('\n');
            var opening = lines[0];
            var fenceStart = opening.TrimStart();
            var fenceCharacter = fenceStart[0];
            var fenceLength = fenceStart.TakeWhile(character => character == fenceCharacter).Count();
            var closing = new string(fenceCharacter, fenceLength);
            var lastLine = lines[^1].Trim();
            var hasClosingFence = lines.Length > 1
                && lastLine.All(character => character == fenceCharacter)
                && lastLine.Length >= fenceLength;
            var numberOfFenceLines = hasClosingFence ? 2 : 1;
            var body = string.Join("\n", lines.Skip(1).Take(lines.Length - numberOfFenceLines));

            // Each fragment gets its own opening line (including the language) and closing fence.
            var wrapperCost = tokenizer.CountTokens(opening + "\n\n" + closing) + FormattingTokenAllowance;
            if (wrapperCost < budget / 2)
            {
                foreach (var part in SplitText(body, budget - wrapperCost, true, cancellationToken))
                {
                    yield return new(opening + "\n" + part + "\n" + closing, false);
                }

                yield break;
            }
        }

        if (block is Table)
        {
            var lines = raw.Split('\n');
            if (lines.Length >= 3)
            {
                // Repeat the column names and separator so each row fragment has its table context.
                var header = lines[0] + "\n" + lines[1] + "\n";
                var headerCost = tokenizer.CountTokens(header) + FormattingTokenAllowance;
                if (headerCost < budget / 2)
                {
                    foreach (var row in lines.Skip(2))
                    {
                        foreach (var part in SplitText(row, budget - headerCost, true, cancellationToken))
                        {
                            yield return new(header + part, false);
                        }
                    }

                    yield break;
                }
            }
        }

        // Paragraphs prefer sentence boundaries; other oversized structures prefer whole lines.
        foreach (var part in SplitText(raw, budget, block is not ParagraphBlock, cancellationToken))
        {
            yield return new(part, block is ParagraphBlock);
        }
    }

    /// <summary>
    /// Reads text in token-limited windows and moves each cut to a suitable boundary when possible.
    /// </summary>
    /// <param name="text">The text to divide without adding document context or Markdown wrappers.</param>
    /// <param name="budget">The token allowance for each piece of this text.</param>
    /// <param name="preferLines">Prefer line endings for structured content; otherwise prefer sentence endings.</param>
    /// <param name="cancellationToken">Stops processing before generating the next piece.</param>
    /// <returns>Nonempty, trimmed pieces in their original order.</returns>
    /// <remarks>
    /// The tokenizer returns character positions, not token IDs. If no suitable boundary exists,
    /// the token-limited cut is retained. Surrogate pairs are kept together so a cut does not split
    /// an emoji or another character represented by two UTF-16 code units.
    /// </remarks>
    private IEnumerable<string> SplitText(string text, int budget, bool preferLines, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Find the largest prefix of the remaining text that fits this piece's token budget.
            var length = tokenizer.GetIndexByTokenCount(text.AsSpan(offset), budget, out _, out _);

            // Back up if the proposed cut would leave the first half of a surrogate pair behind.
            if (length > 0 && offset + length < text.Length && char.IsHighSurrogate(text[offset + length - 1]))
            {
                length--;
            }

            if (length <= 0)
            {
                // Refuse an empty cut: otherwise offset would never advance and the loop would not finish.
                throw new DocumentChunkingException(DocumentChunkingErrorCodes.InvalidChunks);
            }

            if (offset + length < text.Length)
            {
                // The final piece needs no boundary adjustment because all remaining text already fits.
                length = PreferredBoundary(text, offset, length, preferLines);
            }

            var part = text.Substring(offset, length).Trim();
            if (part.Length > 0)
            {
                yield return part;
            }

            offset += length;
        }
    }

    /// <summary>
    /// Finds a readable cut near the end of a token-limited text window.
    /// </summary>
    /// <param name="text">The full text being split.</param>
    /// <param name="offset">The zero-based UTF-16 character position where the current window starts.</param>
    /// <param name="length">The proposed window length, already limited by the tokenizer.</param>
    /// <param name="preferLines">Search for a line ending instead of a sentence ending.</param>
    /// <returns>A length relative to offset, including the selected boundary character.</returns>
    /// <remarks>
    /// Search only the latter half of the window to avoid producing unnecessarily small pieces.
    /// Prefer the requested boundary, then whitespace, then the original cut. The caller invokes
    /// this only when text continues after the window, so reading the next character is safe.
    /// </remarks>
    private static int PreferredBoundary(string text, int offset, int length, bool preferLines)
    {
        var earliestPreferredIndex = length / 2;

        // Search backwards so the selected boundary stays as close as possible to the size limit.
        for (var index = length - 1; index >= earliestPreferredIndex; index--)
        {
            var currentCharacter = text[offset + index];
            if (preferLines)
            {
                if (currentCharacter == '\n')
                {
                    return index + 1;
                }
            }
            else
            {
                var nextCharacter = text[offset + index + 1];
                var endsSentence = IsSentenceEnd(currentCharacter) && char.IsWhiteSpace(nextCharacter);
                if (endsSentence)
                {
                    return index + 1;
                }
            }
        }

        // If no line/sentence ending fits, prefer a word boundary over splitting a word.
        for (var index = length - 1; index >= earliestPreferredIndex; index--)
        {
            if (char.IsWhiteSpace(text[offset + index]))
            {
                return index + 1;
            }
        }

        // Long words or text without whitespace must use the tokenizer's original cut.
        return length;
    }

    /// <summary>
    /// Selects trailing prose to repeat at the start of the next chunk in the same section.
    /// </summary>
    /// <param name="text">The last prose piece of the chunk that has just reached its target size.</param>
    /// <param name="budget">The maximum tokens to copy; zero disables overlap.</param>
    /// <returns>A trimmed suffix, or an empty string when no suitable text remains.</returns>
    /// <remarks>
    /// When the token-limited suffix begins partway through the text, prefer starting after a sentence
    /// ending, then at the next word. The caller decides whether the overlap and new piece fit
    /// together under the hard chunk limit; headings flush the buffer, preventing cross-section overlap.
    /// </remarks>
    private string Overlap(string text, int budget)
    {
        if (budget == 0)
        {
            return string.Empty;
        }

        // Work backwards from the end, because continuity needs the most recent prose.
        var start = tokenizer.GetIndexByTokenCountFromEnd(text, budget, out _, out _);
        if (start > 0 && start < text.Length && char.IsLowSurrogate(text[start]))
        {
            start++;
        }

        if (start > 0)
        {
            // Prefer a complete trailing sentence; otherwise start at the next word.
            for (var index = start; index + 1 < text.Length; index++)
            {
                var endsSentence = IsSentenceEnd(text[index]) && char.IsWhiteSpace(text[index + 1]);
                if (endsSentence)
                {
                    var remainingText = text[(index + 1)..];
                    if (!string.IsNullOrWhiteSpace(remainingText))
                    {
                        return remainingText.Trim();
                    }
                }
            }

            while (start < text.Length && !char.IsWhiteSpace(text[start]))
            {
                start++;
            }
        }

        return text[start..].Trim();
    }

    /// <summary>
    /// Creates a bounded document/section label that gives an individual chunk its source context.
    /// </summary>
    /// <param name="documentName">The source file name.</param>
    /// <param name="headingPath">The current heading hierarchy, or null for text without headings.</param>
    /// <param name="settings">Provides the target size used to reserve a portion of the chunk for context.</param>
    /// <returns>The token-limited label followed by a blank line separating it from the body.</returns>
    /// <remarks>
    /// Context uses at most one third of the target token budget before the separating blank line.
    /// The body budget is calculated separately, and EmitChunk counts the complete final content.
    /// A long label can be truncated, while the separately stored heading path remains available.
    /// </remarks>
    private string ContextPrefix(string documentName, string? headingPath, DocumentChunkingSettings settings)
    {
        var context = $"Document: {documentName}";
        if (!string.IsNullOrEmpty(headingPath))
        {
            context += $"\nSection: {headingPath}";
        }

        // Leave most of the target budget available for document content rather than labels.
        var contextTokenBudget = settings.TargetTokens / ContextTokenBudgetDivisor;
        var length = tokenizer.GetIndexByTokenCount(context, contextTokenBudget, out _, out _);

        if (length > 0 && char.IsHighSurrogate(context[length - 1]))
        {
            length--;
        }

        return context[..length] + "\n\n";
    }

    private static string LimitCharacters(string value, int maximum)
    {
        if (value.Length <= maximum)
        {
            return value;
        }

        var length = char.IsHighSurrogate(value[maximum - 1]) ? maximum - 1 : maximum;

        return value[..length];
    }

    private static bool IsSentenceEnd(char character) => character is '.' or '!' or '?' or '。' or '！' or '？';

    private sealed record Piece(string Text, bool Prose);
}
