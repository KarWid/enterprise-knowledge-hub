namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public interface IDocumentBlobStorage
{
    Task<Uri> CreateUploadUriAsync(
        string blobReference,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<DocumentUploadInspection?> InspectUploadAsync(
        string blobReference,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(string blobReference, CancellationToken cancellationToken);
}

public sealed record DocumentUploadInspection(long ContentLength, bool HasPdfSignature);
