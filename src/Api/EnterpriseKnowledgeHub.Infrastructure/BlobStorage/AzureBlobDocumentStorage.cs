using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Infrastructure.BlobStorage;

internal sealed class AzureBlobDocumentStorage(
    BlobServiceClient serviceClient,
    BlobContainerClient containerClient,
    IEnumerable<IDocumentContentValidator> contentValidators) : IDocumentBlobStorage
{
    private readonly IReadOnlyDictionary<DocumentFormat, IDocumentContentValidator> validatorsByFormat =
        contentValidators.ToDictionary(validator => validator.Format);

    public async Task<Uri> CreateUploadUriAsync(
        string blobReference,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var userDelegationKey = await serviceClient.GetUserDelegationKeyAsync(
            startsOn: DateTimeOffset.UtcNow.AddMinutes(-5),
            expiresOn: expiresAt,
            cancellationToken);
       
        var blob = containerClient.GetBlobClient(blobReference);
        var sas = new BlobSasBuilder
        {
            BlobContainerName = containerClient.Name,
            BlobName = blobReference,
            Resource = "b",
            ExpiresOn = expiresAt,
            Protocol = SasProtocol.Https
        };
        // Create permits a new blob but cannot overwrite an existing one.
        sas.SetPermissions(BlobSasPermissions.Create);

        return blob.GenerateUserDelegationSasUri(sas, userDelegationKey.Value);
    }

    public async Task<DocumentUploadInspection?> InspectUploadAsync(
        string blobReference,
        long maximumSizeBytes,
        CancellationToken cancellationToken,
        string? expectedETag = null)
    {
        var blob = containerClient.GetBlobClient(blobReference);

        try
        {
            var blobRequestConditions = expectedETag is null 
                ? null 
                : new BlobRequestConditions { IfMatch = new ETag(expectedETag) };

            var properties = await blob.GetPropertiesAsync(conditions: blobRequestConditions, cancellationToken: cancellationToken);
            var length = properties.Value.ContentLength;
            var etag = properties.Value.ETag.ToString();
            // Reject size before downloading. Browser MIME headers are not format evidence.
            if (length <= 0 || length > maximumSizeBytes)
            {
                return new DocumentUploadInspection(length, false, etag, "invalid_size");
            }

            var format = DocumentFormat.FromFileName(blobReference);
            // Every supported format requires an explicitly registered content validator.
            if (format is null || !validatorsByFormat.TryGetValue(format, out var validator))
            {
                return new DocumentUploadInspection(length, false, etag, "unsupported_format");    
            }

            var download = await blob.DownloadStreamingAsync(
                new BlobDownloadOptions
                {
                    Range = new HttpRange(0, Math.Min(length, validator.MaximumInspectionBytes ?? length)),
                    Conditions = new BlobRequestConditions
                    {
                        IfMatch = properties.Value.ETag
                    }
                },
                cancellationToken);

            await using var content = download.Value.Content;

            var valid = await validator.ValidateAsync(content, length, cancellationToken);

            return new DocumentUploadInspection(length, valid, etag, valid ? null : "invalid_format");
        }
        catch (RequestFailedException exception) when (exception.Status == 404 && exception.ErrorCode == "BlobNotFound")
        {
            return null;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new DocumentBlobChangedException();
        }
    }

    public async Task<string?> GetUploadETagAsync(string blobReference, CancellationToken cancellationToken)
    {
        try
        {
            // Cleanup needs identity only; malformed content must never prevent deletion.
            var properties = await containerClient
                .GetBlobClient(blobReference)
                .GetPropertiesAsync(cancellationToken: cancellationToken);

            return properties.Value.ETag.ToString();
        }
        catch (RequestFailedException exception) when (exception.Status == 404 && exception.ErrorCode == "BlobNotFound")
        {
            return null;
        }
    }

    public async Task DeleteIfExistsAsync(string blobReference, string etag, CancellationToken cancellationToken)
    {
        await containerClient
            .GetBlobClient(blobReference)
            .DeleteIfExistsAsync(conditions: new BlobRequestConditions { IfMatch = new ETag(etag) },
                cancellationToken: cancellationToken);
    }
}
