using System.Net;
using System.Text;
using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using EnterpriseKnowledgeHub.Infrastructure.BlobStorage;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class BlobInspectionTests
{
    private const string BlobReference = "reserved.pdf";
    private const string ContainerName = "documents";
    private const string ExpectedETag = "\"0x123\"";
    private const long BlobLength = 100;

    [Fact]
    public async Task PropertyAndSignatureReadsUseTheSameETag()
    {
        // Arrange
        var requestedETags = new List<string?>();
        using var httpClient = CreateHttpClient(request =>
        {
            requestedETags.Add(request.Headers.IfMatch.SingleOrDefault()?.Tag);
            return CreateBlobInspectionResponse(request);
        });
        var storage = CreateStorage(httpClient);

        // Act
        var inspection = await storage.InspectUploadAsync(
            BlobReference,
            CancellationToken.None,
            ExpectedETag);

        // Assert
        Assert.NotNull(inspection);
        Assert.True(inspection.HasPdfSignature);
        Assert.Equal(BlobLength, inspection.ContentLength);
        Assert.Equal(ExpectedETag, inspection.ETag);
        Assert.Equal([ExpectedETag, ExpectedETag], requestedETags);
    }

    [Fact]
    public async Task MissingBlobIsTreatedAsAbsent()
    {
        // Arrange
        using var httpClient = CreateHttpClient(_ => CreateNotFoundResponse("BlobNotFound"));
        var storage = CreateStorage(httpClient);

        // Act
        var inspection = await storage.InspectUploadAsync(BlobReference, CancellationToken.None);

        // Assert
        Assert.Null(inspection);
    }

    [Fact]
    public async Task MissingContainerIsPropagatedAsStorageFailure()
    {
        // Arrange
        using var httpClient = CreateHttpClient(_ => CreateNotFoundResponse("ContainerNotFound"));
        var storage = CreateStorage(httpClient);

        // Act
        var inspect = () => storage.InspectUploadAsync(BlobReference, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RequestFailedException>(inspect);
    }

    [Fact]
    public async Task UserDelegationSasIsHttpsSingleBlobCreateOnlyAndHasNoStartDelay()
    {
        // Arrange
        var startsAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        using var httpClient = CreateHttpClient(_ => CreateUserDelegationKeyResponse(startsAt, expiresAt));
        var storage = CreateStorage(httpClient);

        // Act
        var uploadUri = await storage.CreateUploadUriAsync(
            BlobReference,
            expiresAt,
            CancellationToken.None);

        // Assert
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uploadUri.Query);
        Assert.True(query["sp"] == "c",
            "The SAS 'sp' value must contain only 'c' (create), so the browser cannot overwrite an existing blob.");
        Assert.True(query["sr"] == "b",
            "The SAS 'sr' value must be 'b' (blob), so permission is not delegated for the entire container.");
        Assert.True(query["spr"] == "https",
            "The SAS 'spr' value must require HTTPS so the token and document cannot be sent over plain HTTP.");
        Assert.False(query.ContainsKey("st"),
            "The SAS must omit 'st' (starts on) so it is usable immediately despite clock differences between systems.");
        Assert.True(uploadUri.AbsolutePath == $"/{ContainerName}/{BlobReference}",
            "The SAS URI must identify only the exact blob reserved by the application.");
    }

    private static AzureBlobDocumentStorage CreateStorage(HttpClient httpClient)
    {
        var serviceClient = new BlobServiceClient(
            new Uri("https://teststore.blob.core.windows.net"),
            new BlobClientOptions
            {
                Transport = new HttpClientTransport(httpClient),
                Retry = { MaxRetries = 0 }
            });

        return new AzureBlobDocumentStorage(
            serviceClient,
            serviceClient.GetBlobContainerClient(ContainerName));
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond));

    private static HttpResponseMessage CreateBlobInspectionResponse(HttpRequestMessage request)
    {
        var isPropertiesRequest = request.Method == HttpMethod.Head;
        var content = isPropertiesRequest ? new byte[BlobLength] : "%PDF-"u8.ToArray();
        var response = new HttpResponseMessage(
            isPropertiesRequest ? HttpStatusCode.OK : HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent(content)
        };

        response.Headers.ETag = new(ExpectedETag);
        response.Headers.Add("x-ms-blob-type", "BlockBlob");
        response.Headers.Add("x-ms-request-id", "test");
        response.Content.Headers.ContentLength = content.Length;
        response.Content.Headers.LastModified = DateTimeOffset.UtcNow;

        if (!isPropertiesRequest)
        {
            response.Content.Headers.ContentRange = new(0, content.Length - 1, BlobLength);
        }

        return response;
    }

    private static HttpResponseMessage CreateNotFoundResponse(string errorCode)
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        response.Headers.Add("x-ms-error-code", errorCode);
        return response;
    }

    private static HttpResponseMessage CreateUserDelegationKeyResponse(
        DateTimeOffset startsAt,
        DateTimeOffset expiresAt) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent($"""
                <?xml version="1.0" encoding="utf-8"?>
                <UserDelegationKey>
                  <SignedOid>{Guid.NewGuid()}</SignedOid>
                  <SignedTid>{Guid.NewGuid()}</SignedTid>
                  <SignedStart>{startsAt:yyyy-MM-ddTHH:mm:ssZ}</SignedStart>
                  <SignedExpiry>{expiresAt:yyyy-MM-ddTHH:mm:ssZ}</SignedExpiry>
                  <SignedService>b</SignedService>
                  <SignedVersion>2023-11-03</SignedVersion>
                  <Value>{Convert.ToBase64String(new byte[32])}</Value>
                </UserDelegationKey>
                """, Encoding.UTF8, "application/xml")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
