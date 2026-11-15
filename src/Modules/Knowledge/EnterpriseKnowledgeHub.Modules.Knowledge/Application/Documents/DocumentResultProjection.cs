using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed record DocumentTextExtractionStatusProjection(
    TextExtractionStatus Status,
    int? CharacterCount,
    DateTime? CompletedAt,
    string? LastError);

internal sealed record DocumentChunkingStatusProjection(
    DocumentChunkingStatus Status,
    int? ChunkCount,
    DateTime? CompletedAt,
    string? LastError);

internal sealed record DocumentResultProjection(
    Guid Id,
    string Name,
    string ContentType,
    DocumentStatus Status,
    DateTime CreatedAt,
    string? ValidatedETag,
    DocumentTextExtractionStatusProjection? TextExtraction,
    DocumentChunkingStatusProjection? Chunking)
{
    public DocumentResult ToResult(DocumentTextExtractionStatusResolver extractionStatusResolver) =>
        new(
            Id,
            Name,
            ContentType,
            Status,
            CreatedAt,
            ValidatedETag,
            extractionStatusResolver.Resolve(Status, Name, ValidatedETag, TextExtraction));

    public DocumentResult ToResult(DocumentTextExtractionStatusResolver extractionStatusResolver,
        DocumentChunkingStatusResolver chunkingStatusResolver) =>
        ToResult(extractionStatusResolver) with
        {
            Chunking = chunkingStatusResolver.Resolve(Status, Name, ValidatedETag, TextExtraction?.Status, Chunking)
        };
}

internal static class DocumentResultProjectionExtensions
{
    // Basic view: extraction status is available without fetching chunking diagnostics.
    public static IQueryable<DocumentResultProjection> SelectDocumentResults(
        this IQueryable<Document> documents,
        IQueryable<DocumentTextExtraction> extractions) =>
        from document in documents
        join extraction in extractions
            on new { document.OrganizationId, DocumentId = document.Id, SourceETag = document.ValidatedETag }
            equals new { extraction.OrganizationId, extraction.DocumentId, SourceETag = (string?)extraction.SourceETag }
            into matchingExtractions
        from extraction in matchingExtractions.DefaultIfEmpty()
        select new DocumentResultProjection(
            document.Id, document.Name, document.ContentType, document.Status, document.CreatedAt, document.ValidatedETag,
            extraction == null ? null : new DocumentTextExtractionStatusProjection(
                extraction.Status, extraction.CharacterCount, extraction.CompletedAt, extraction.LastError),
            null);

    // Full view: callers must authorize access to chunking diagnostics before selecting this overload.
    public static IQueryable<DocumentResultProjection> SelectDocumentResults(
        this IQueryable<Document> documents,
        IQueryable<DocumentTextExtraction> extractions,
        IQueryable<DocumentChunking> currentChunkings) =>
        from document in documents
        join extraction in extractions
            on new { document.OrganizationId, DocumentId = document.Id, SourceETag = document.ValidatedETag }
            equals new { extraction.OrganizationId, extraction.DocumentId, SourceETag = (string?)extraction.SourceETag }
            into matchingExtractions
        from extraction in matchingExtractions.DefaultIfEmpty()
        join chunking in currentChunkings
            on new
            {
                document.OrganizationId,
                DocumentId = document.Id,
                ExtractionId = extraction == null ? (Guid?)null : extraction.Id,
                SourceETag = document.ValidatedETag,
                FormatId = extraction == null ? null : extraction.FormatId
            }
            equals new
            {
                chunking.OrganizationId,
                chunking.DocumentId,
                ExtractionId = (Guid?)chunking.ExtractionId,
                SourceETag = (string?)chunking.SourceETag,
                FormatId = (string?)chunking.FormatId
            }
            into matchingChunkings
        from chunking in matchingChunkings.DefaultIfEmpty()
        select new DocumentResultProjection(
            document.Id,
            document.Name,
            document.ContentType,
            document.Status,
            document.CreatedAt,
            document.ValidatedETag,
            extraction == null
                ? null
                : new DocumentTextExtractionStatusProjection(extraction.Status, extraction.CharacterCount, extraction.CompletedAt, extraction.LastError),
            chunking == null
                ? null
                : new DocumentChunkingStatusProjection(chunking.Status, chunking.ChunkCount, chunking.CompletedAt, chunking.LastError));
}
