using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

namespace EnterpriseKnowledgeHub.Infrastructure.BlobStorage;

internal sealed class AzureBlobDocumentStorage(
    BlobServiceClient serviceClient,
    BlobContainerClient containerClient) : IDocumentBlobStorage
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

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
        CancellationToken cancellationToken)
    {
        var blob = containerClient.GetBlobClient(blobReference);

        try
        {
            var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

            if (properties.Value.ContentLength < PdfSignature.Length)
            {
                return new DocumentUploadInspection(properties.Value.ContentLength, false);
            }

            var download = await blob.DownloadStreamingAsync(
                new BlobDownloadOptions
                {
                    Range = new HttpRange(0, PdfSignature.Length),
                    Conditions = new BlobRequestConditions
                    {
                        IfMatch = properties.Value.ETag
                    }
                },
                cancellationToken);

            await using var content = download.Value.Content;
            var header = new byte[PdfSignature.Length];
            var bytesRead = 0;
            while (bytesRead < header.Length)
            {
                var read = await content.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
                if (read == 0)
                    break;

                bytesRead += read;
            }

            return new DocumentUploadInspection(
                properties.Value.ContentLength,
                bytesRead == PdfSignature.Length && header.SequenceEqual(PdfSignature));
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteIfExistsAsync(string blobReference, CancellationToken cancellationToken)
    {
        await containerClient
            .GetBlobClient(blobReference)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
