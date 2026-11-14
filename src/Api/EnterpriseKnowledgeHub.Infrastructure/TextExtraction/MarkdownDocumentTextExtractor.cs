using System.Text;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Infrastructure.TextExtraction;

internal sealed class MarkdownDocumentTextExtractor : IDocumentTextExtractor
{
    public DocumentFormat Format => DocumentFormat.Markdown;
    public string Version => "markdown-utf8-v1";

    public async Task<string> ExtractAsync(Stream content, long contentLength, int maximumCharacters, CancellationToken cancellationToken)
    {
        if (contentLength <= 0)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.EmptyText);
        }

        var encoding = new UTF8Encoding(false, true);
        var decoder = encoding.GetDecoder();
        var bytes = new byte[8192];
        var characters = new char[encoding.GetMaxCharCount(bytes.Length)];
        var text = new StringBuilder();
        var remaining = contentLength;
        var firstCharacter = true;
        var previousCarriageReturn = false;
        var hasText = false;

        try
        {
            while (remaining > 0)
            {
                var read = await content.ReadAsync(bytes.AsMemory(0, (int)Math.Min(bytes.Length, remaining)), cancellationToken);
                if (read == 0)
                {
                    // An interrupted download is retryable, unlike malformed content.
                    throw new EndOfStreamException();
                }

                remaining -= read;
                var count = decoder.GetChars(bytes, 0, read, characters, 0, flush: remaining == 0);
                for (var index = 0; index < count; index++)
                {
                    var character = characters[index];
                    if (firstCharacter)
                    {
                        firstCharacter = false;
                        if (character == '\uFEFF')
                        {
                            continue;
                        }
                    }

                    if (char.IsControl(character) && character is not ('\t' or '\r' or '\n'))
                    {
                        throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.InvalidTextContent);
                    }

                    if (character == '\n' && previousCarriageReturn)
                    {
                        previousCarriageReturn = false;
                        continue;
                    }

                    previousCarriageReturn = character == '\r';
                    if (text.Length >= maximumCharacters)
                    {
                        throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.TextLimitExceeded);
                    }

                    text.Append(character == '\r' ? '\n' : character);
                    hasText |= !char.IsWhiteSpace(character) && character != '\uFEFF';
                }
            }
        }
        catch (DecoderFallbackException)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.InvalidUtf8);
        }

        if (!hasText)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.EmptyText);
        }

        return text.ToString();
    }
}
