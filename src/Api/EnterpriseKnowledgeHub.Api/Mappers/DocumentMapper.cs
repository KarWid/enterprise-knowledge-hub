using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

namespace EnterpriseKnowledgeHub.Api.Mappers;

internal static class DocumentMapper
{
    internal static DocumentResponse MapToDocumentResponse(DocumentResult document) => new(
        document.Id,
        document.Name,
        document.ContentType,
        document.Status.ToString(),
        document.CreatedAt);

    internal static IReadOnlyList<DocumentResponse> MapToDocumentResponses(
        IEnumerable<DocumentResult> documents) =>
        [.. documents.Select(MapToDocumentResponse)];

    internal static DocumentUploadSessionResponse MapToDocumentUploadSessionResponse(
        DocumentUploadSessionResult upload) => new(
        upload.Document.Id,
        upload.UploadUri,
        upload.ExpiresAt);
}
