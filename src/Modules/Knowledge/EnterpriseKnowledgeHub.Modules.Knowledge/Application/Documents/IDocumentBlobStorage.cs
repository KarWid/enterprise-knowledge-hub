namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public interface IDocumentBlobStorage
{
    Task<Uri> CreateUploadUriAsync(
        string blobReference,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<DocumentUploadInspection?> InspectUploadAsync(
        string blobReference,
        CancellationToken cancellationToken,
        string? expectedETag = null);

    Task DeleteIfExistsAsync(string blobReference, string etag, CancellationToken cancellationToken);
}

public sealed record DocumentUploadInspection(long ContentLength, bool HasPdfSignature, string ETag);

// A stale event must not validate or delete the replacement bytes.
public sealed class DocumentBlobChangedException : Exception;
