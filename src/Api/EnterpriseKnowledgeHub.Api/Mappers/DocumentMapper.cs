using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentUploadCapabilities;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentTextPreview;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

namespace EnterpriseKnowledgeHub.Api.Mappers;

internal static class DocumentMapper
{
    internal static DocumentResponse MapToDocumentResponse(DocumentResult document) => new(
        document.Id,
        document.Name,
        document.ContentType,
        document.Status.ToString(),
        document.CreatedAt,
        document.TextExtraction is { } extraction
            ? new DocumentTextExtractionResponse(extraction.Status, extraction.CharacterCount, extraction.CompletedAt, extraction.FailureCode)
            : null,
        document.Chunking is { } chunking
            ? new DocumentChunkingResponse(chunking.Status, chunking.ChunkCount, chunking.CompletedAt, chunking.FailureCode)
            : null);

    internal static DocumentChunksResponse MapToDocumentChunksResponse(DocumentChunksResult chunks) =>
        new([.. chunks.Items.Select(MapToDocumentChunkPreviewResponse)],
            chunks.ChunkCount, chunks.Offset, chunks.HasMore);

    internal static DocumentChunkPreviewResponse MapToDocumentChunkPreviewResponse(DocumentChunkPreviewResult chunk) =>
        new(chunk.Id, chunk.Ordinal, chunk.Content, chunk.HeadingPath, chunk.TokenCount, chunk.IsTruncated);

    internal static DocumentTextPreviewResponse MapToDocumentTextPreviewResponse(DocumentTextPreviewResult preview) =>
        new(preview.Text, preview.CharacterCount, preview.IsTruncated, preview.CompletedAt);

    internal static IReadOnlyList<DocumentResponse> MapToDocumentResponses(IEnumerable<DocumentResult> documents) =>
        [.. documents.Select(MapToDocumentResponse)];

    internal static DocumentUploadSessionResponse MapToDocumentUploadSessionResponse(DocumentUploadSessionResult upload) => 
        new (
        upload.Document.Id,
        upload.UploadUri,
        upload.ExpiresAt,
        upload.Document.ContentType);

    internal static DocumentUploadCapabilitiesResponse MapToDocumentUploadCapabilitiesResponse(
        DocumentUploadCapabilitiesResult capabilities) => new(
        [.. capabilities.Formats.Select(MapToDocumentUploadFormatResponse)],
        capabilities.MaxSizeBytes);

    internal static DocumentUploadFormatResponse MapToDocumentUploadFormatResponse(DocumentUploadFormatResult format) =>
        new DocumentUploadFormatResponse(format.Id, format.Extension, format.ContentType);
}
