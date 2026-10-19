using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using EnterpriseKnowledgeHub.Infrastructure.BlobStorage;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

/// <summary>
/// Exercises the document upload flow against the local Azurite Blob Storage emulator.
/// The database and API still run inside the integration-test process, while document
/// bytes travel through Azurite using the same HTTP PUT flow used by the browser.
/// </summary>
/// <remarks>
/// Azurite cannot reproduce every Azure feature. This test uses an emulator-only service
/// SAS instead of the production user delegation SAS, and it submits the BlobCreated
/// notification manually because Azurite does not deliver Azure Event Grid events.
/// </remarks>
public sealed class AzuriteUploadTests
{
    // The webhook is hosted by WebApplicationFactory; no external API process is required.
    private const string WebhookPath = "/webhooks/storage-events";

    // The finalizer currently checks the PDF signature, so a small deterministic fixture is sufficient.
    private static readonly byte[] PdfContents = "%PDF-1.7\nlocal integration fixture"u8.ToArray();

    [AzuriteFact]
    public async Task DirectPdfUpload_WithCreateOnlySas_IsFinalizedAfterStorageNotification()
    {
        // Arrange
        // "UseDevelopmentStorage=true" points the Azure SDK at the standard local Azurite endpoints.
        var blobServiceClient = CreateAzuriteBlobServiceClient();

        // A unique container isolates this execution from concurrent and previous test runs.
        var container = blobServiceClient.GetBlobContainerClient(
            $"ekh-upload-test-{Guid.NewGuid():N}");
        await container.CreateAsync();

        try
        {
            using var factory = new StorageWebhookFactory
            {
                ContainerName = container.Name
            };
            await using var db = factory.Database.CreateContext();

            var document = await UploadFinalizationTests.Reserve(db);
            var blob = container.GetBlobClient(document.BlobReference);
            var uploadUri = CreateCreateOnlyUploadUri(blob, container.Name);
            var inbox = factory.Database.Inbox(db);
            var finalizer = CreateFinalizer(factory, db, blobServiceClient, container, inbox);

            using var storageClient = new HttpClient();
            using var webhookClient = CreateAuthenticatedWebhookClient(factory);

            // Act
            // Upload the PDF directly and probe the SAS restrictions.
            // This first request represents the browser's successful direct upload.
            using var uploadRequest = CreateUploadRequest(uploadUri);
            using var uploadResponse = await storageClient.SendAsync(uploadRequest);

            // Repeating the conditional PUT must not replace the blob that now exists.
            using var duplicateUploadRequest = CreateUploadRequest(uploadUri);
            using var duplicateUploadResponse = await storageClient.SendAsync(duplicateUploadRequest);

            // Removing If-None-Match proves that the SAS permission itself also prevents overwrite.
            using var overwriteRequest = CreateUploadRequest(uploadUri);
            overwriteRequest.Headers.Remove("If-None-Match");
            using var overwriteResponse = await storageClient.SendAsync(overwriteRequest);

            // The same SAS must not allow the browser to read or delete the uploaded document.
            using var readResponse = await storageClient.GetAsync(uploadUri);
            using var deleteResponse = await storageClient.DeleteAsync(uploadUri);

            // Deliver the storage notification and process the resulting inbox work.
            // The ETag binds the notification and finalization to this exact blob version.
            var uploadedETag = uploadResponse.Headers.ETag?.Tag
                ?? throw new InvalidOperationException("Azurite did not return the uploaded blob ETag.");
            var notification = CreateBlobCreatedNotification(
                container.Name,
                blob.Name,
                uploadedETag);
            using var webhookResponse = await webhookClient.PostAsJsonAsync(
                WebhookPath,
                notification);

            // Event Grid timestamps use real time, so move the controllable test clock beyond creation time.
            factory.Database.Clock.Now = DateTimeOffset.UtcNow.AddSeconds(1);
            var workItem = await inbox.ClaimAsync(CancellationToken.None)
                ?? throw new InvalidOperationException("The storage notification did not create claimable inbox work.");

            await finalizer.ProcessAsync(workItem, CancellationToken.None);
            var completedItems = await inbox.CompleteAsync(workItem, CancellationToken.None);
            var persistedDocument = await db.Documents.AsNoTracking().SingleAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
            Assert.False(
                duplicateUploadResponse.IsSuccessStatusCode,
                "If-None-Match must prevent a second PUT from replacing the existing blob.");
            Assert.False(
                overwriteResponse.IsSuccessStatusCode,
                "Create-only SAS permission must deny an overwrite even without If-None-Match.");
            Assert.False(
                readResponse.IsSuccessStatusCode,
                "The upload SAS must not grant permission to read document contents.");
            Assert.False(
                deleteResponse.IsSuccessStatusCode,
                "The upload SAS must not grant permission to delete the blob.");
            Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);
            Assert.Equal(1, completedItems);
            Assert.Equal(DocumentStatus.Uploaded, persistedDocument.Status);
        }
        finally
        {
            // Cleanup runs even when an assertion or an intermediate request fails.
            await container.DeleteIfExistsAsync();
        }
    }

    private static BlobServiceClient CreateAzuriteBlobServiceClient()
    {
        return new BlobServiceClient(
            "UseDevelopmentStorage=true",
            new BlobClientOptions
            {
                Retry =
                {
                    MaxRetries = 0
                }
            });
    }

    private static Uri CreateCreateOnlyUploadUri(BlobClient blob, string containerName)
    {
        // Azurite uses an emulator-only service SAS over HTTP. Production uses an HTTPS
        // user delegation SAS generated with the API's managed identity.
        var sas = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blob.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5),
            Protocol = SasProtocol.HttpsAndHttp
        };
        sas.SetPermissions(BlobSasPermissions.Create);

        return blob.GenerateSasUri(sas);
    }

    private static HttpRequestMessage CreateUploadRequest(Uri uploadUri)
    {
        // Azure Blob Storage requires the blob type header for a single Put Blob operation.
        var request = new HttpRequestMessage(HttpMethod.Put, uploadUri)
        {
            Content = new ByteArrayContent(PdfContents)
        };

        // The conditional header complements create-only SAS permission and prevents replacement.
        request.Headers.Add("If-None-Match", "*");
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        request.Headers.Add("x-ms-blob-content-type", "application/pdf");
        request.Headers.Add("x-ms-version", "2023-11-03");

        return request;
    }

    private static HttpClient CreateAuthenticatedWebhookClient(StorageWebhookFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.Token());
        return client;
    }

    private static object[] CreateBlobCreatedNotification(
        string containerName,
        string blobName,
        string eTag)
    {
        // Azurite stores the blob but does not invoke Event Grid. Build the Event Grid schema
        // explicitly and send it through the authenticated webhook test host instead.
        return
        [
            new
            {
                id = Guid.NewGuid().ToString(),
                topic = StorageWebhookFactory.Source,
                eventType = "Microsoft.Storage.BlobCreated",
                subject = $"/blobServices/default/containers/{containerName}/blobs/{blobName}",
                data = new
                {
                    api = "PutBlob",
                    blobType = "BlockBlob",
                    eTag,
                    url = $"https://teststore.blob.core.windows.net/{containerName}/{blobName}"
                }
            }
        ];
    }

    private static DocumentUploadFinalizer CreateFinalizer(
        StorageWebhookFactory factory,
        KnowledgeDbContext db,
        BlobServiceClient serviceClient,
        BlobContainerClient container,
        IUploadInbox inbox)
    {
        // Use the real finalizer and AzureBlobDocumentStorage against Azurite. Only external
        // Azure identity and Event Grid delivery are substituted by the test environment.
        return new DocumentUploadFinalizer(
            db,
            new AzureBlobDocumentStorage(serviceClient, container),
            inbox,
            factory.Database.Clock,
            Options.Create(new DocumentUploadOptions()),
            Options.Create(factory.Database.Options),
            NullLogger<DocumentUploadFinalizer>.Instance);
    }
}

/// <summary>
/// Marks tests that require a separately running Azurite instance. The test remains opt-in
/// so the normal integration suite does not fail on machines where Azurite is unavailable.
/// </summary>
public sealed class AzuriteFactAttribute : FactAttribute
{
    public AzuriteFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("EKH_TEST_AZURITE") != "1")
        {
            Skip = "Set EKH_TEST_AZURITE=1 and run Azurite on localhost:10000 (see docs/DocumentUploads.md).";
        }
    }
}
