using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

internal sealed record DocumentTextExtractionStatusProjection(
    TextExtractionStatus Status,
    int? CharacterCount,
    DateTime? CompletedAt,
    string? LastError);

internal sealed record DocumentResultProjection(
    Guid Id,
    string Name,
    string ContentType,
    DocumentStatus Status,
    DateTime CreatedAt,
    string? ValidatedETag,
    DocumentTextExtractionStatusProjection? TextExtraction)
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
}

internal static class DocumentResultProjectionExtensions
{
    public static IQueryable<DocumentResultProjection> SelectDocumentResults(
        this IQueryable<Document> documents,
        IQueryable<DocumentTextExtraction> extractions) =>
        documents
            .GroupJoin(
                extractions,
                document => new
                {
                    document.OrganizationId,
                    DocumentId = document.Id,
                    SourceETag = document.ValidatedETag
                },
                extraction => new
                {
                    extraction.OrganizationId,
                    extraction.DocumentId,
                    SourceETag = (string?)extraction.SourceETag
                },
                (document, matchingExtractions) => new
                {
                    Document = document,
                    Extractions = matchingExtractions
                })
            .SelectMany(
                source => source.Extractions.DefaultIfEmpty(),
                (source, extraction) => new DocumentResultProjection(
                    source.Document.Id,
                    source.Document.Name,
                    source.Document.ContentType,
                    source.Document.Status,
                    source.Document.CreatedAt,
                    source.Document.ValidatedETag,
                    extraction == null
                        ? null
                        : new DocumentTextExtractionStatusProjection(
                            extraction.Status,
                            extraction.CharacterCount,
                            extraction.CompletedAt,
                            extraction.LastError)));
}
