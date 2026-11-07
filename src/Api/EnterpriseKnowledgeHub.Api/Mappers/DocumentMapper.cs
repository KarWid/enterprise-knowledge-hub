using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentUploadCapabilities;

namespace EnterpriseKnowledgeHub.Api.Mappers;

internal static class DocumentMapper
{
    internal static DocumentResponse MapToDocumentResponse(DocumentResult document) => new(
        document.Id,
        document.Name,
        document.ContentType,
        document.Status.ToString(),
        document.CreatedAt);

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
