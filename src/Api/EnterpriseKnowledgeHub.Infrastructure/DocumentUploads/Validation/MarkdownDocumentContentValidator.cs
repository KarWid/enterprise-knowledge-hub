using System.Text;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;

internal sealed class MarkdownDocumentContentValidator : IDocumentContentValidator
{
    public DocumentFormat Format => DocumentFormat.Markdown;

    public long? MaximumInspectionBytes => null;

    public async Task<bool> ValidateAsync(Stream content, long contentLength, CancellationToken cancellationToken)
    {
        // Strict decoding across buffer boundaries, including a final incomplete UTF-8 sequence.
        // A UTF-8 BOM is allowed; UTF-16/32 and binary control characters are rejected.
        var encoding = new UTF8Encoding(false, true);
        var decoder = encoding.GetDecoder();
        var bytes = new byte[8192];
        var characters = new char[encoding.GetMaxCharCount(bytes.Length)];
        var remaining = contentLength;
        var hasText = false;
        try
        {
            while (remaining > 0)
            {
                var read = await content.ReadAsync(bytes.AsMemory(0, (int)Math.Min(bytes.Length, remaining)), cancellationToken);
                if (read == 0)
                {
                    throw new EndOfStreamException("The document download ended before its declared length.");   
                }

                remaining -= read;
                var count = decoder.GetChars(bytes, 0, read, characters, 0, flush: remaining == 0);
                
                for (var index = 0; index < count; index++)
                {
                    var character = characters[index];
                    if (char.IsControl(character) && character is not ('\t' or '\r' or '\n'))
                    {
                        return false;
                    }
                    
                    if (!char.IsWhiteSpace(character) && character != '\uFEFF')
                    {
                        hasText = true;    
                    } 
                }
            }
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        return hasText;
    }
}
