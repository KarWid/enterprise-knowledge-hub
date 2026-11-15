using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

internal static class DocumentChunkPreviewMapper
{
    internal const int MaximumPreviewCharacters = DocumentTextExtractionOptions.PreviewCharacterLimit;

    internal static DocumentChunkPreviewResult MapToPreview(ChunkPreviewProjection preview)
    {
        var text = preview.Content;
        if (text.Length > MaximumPreviewCharacters)
        {
            text = text[..MaximumPreviewCharacters];
        }

        // A truncated preview must not end with the first half of a UTF-16 surrogate pair.
        if (text.Length > 0 && char.IsHighSurrogate(text[^1]))
        {
            text = text[..^1];
        }

        var isTruncated = text.Length < preview.Content.Length;
        return new DocumentChunkPreviewResult(
            preview.Id, preview.Ordinal, text, preview.HeadingPath, preview.TokenCount, isTruncated);
    }
}
