using System.Net;
using System.Text;
using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using EnterpriseKnowledgeHub.Infrastructure.BlobStorage;
using EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

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
            DocumentUploadOptions.DefaultMaxUploadSizeBytes,
            CancellationToken.None,
            ExpectedETag);

        // Assert
        Assert.NotNull(inspection);
        Assert.True(inspection.IsValidFormat);
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
        var inspection = await storage.InspectUploadAsync(BlobReference, DocumentUploadOptions.DefaultMaxUploadSizeBytes, CancellationToken.None);

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
        var inspect = () => storage.InspectUploadAsync(BlobReference, DocumentUploadOptions.DefaultMaxUploadSizeBytes, CancellationToken.None);

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

    [Theory]
    [InlineData("%PDF-1.7", true)]
    [InlineData("plain text", false)]
    [InlineData("%PD", false)]
    public async Task PdfStillRequiresItsSignature(string contents, bool expectedValid)
    {
        using var httpClient = CreateHttpClient(request => CreateContentResponse(request, Encoding.UTF8.GetBytes(contents)));

        var result = await CreateStorage(httpClient).InspectUploadAsync("reserved.pdf", 100, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedValid, result.IsValidFormat);
    }

    // Each row names the content rule exercised by its bytes and expected validation result.
    public static TheoryData<string, byte[], bool> MarkdownContents => new()
    {
        { "Accepts UTF-8 text with accented letters, emoji, newlines and tabs", Encoding.UTF8.GetBytes("# Notes\nZażółć gęślą jaźń 📝\n\t- Item"), true },
        { "Accepts a UTF-8 BOM followed by text", new byte[] { 0xEF, 0xBB, 0xBF, (byte)'#', (byte)'A' }, true },
        // The validator reads 8192 bytes at a time; the four-byte emoji spans two reads.
        { "Accepts an emoji split two bytes / two bytes across the read buffer", Encoding.UTF8.GetBytes(new string('a', 8190) + "📝" + new string('b', 8192)), true },
        { "Accepts an emoji split three bytes / one byte across the read buffer", Encoding.UTF8.GetBytes(new string('a', 8189) + "📝" + new string('b', 8192)), true },
        { "Rejects a NUL character after a full buffer of valid text", Encoding.UTF8.GetBytes(new string('a', 8192) + "\0"), false },
        { "Rejects an incomplete UTF-8 sequence at end of stream", new byte[] { (byte)'#', 0xC3 }, false },
        { "Rejects an invalid UTF-8 byte after valid text", new byte[] { (byte)'#', 0xFF }, false },
        { "Rejects a UTF-8 BOM without text", new byte[] { 0xEF, 0xBB, 0xBF }, false },
        { "Rejects whitespace-only content", Encoding.UTF8.GetBytes(" \r\n\t"), false },
        { "Rejects UTF-16 encoded text", Encoding.Unicode.GetBytes("# UTF-16"), false },
        { "Rejects a binary control character between valid letters", new byte[] { (byte)'a', 1, (byte)'b' }, false },
        { "Rejects an empty document", Array.Empty<byte>(), false }
    };

    [Theory]
    [MemberData(nameof(MarkdownContents))]
    public async Task MarkdownRequiresNonemptyStrictUtf8Text(string scenario, byte[] bytes, bool expectedValid)
    {
        // Arrange
        var requestedETags = new List<string?>();
        using var httpClient = CreateHttpClient(request =>
        {
            requestedETags.Add(request.Headers.IfMatch.SingleOrDefault()?.Tag);
            return CreateContentResponse(request, bytes);
        });
        var storage = CreateStorage(httpClient);

        // Act
        var result = await storage.InspectUploadAsync("reserved.md", 30_000, CancellationToken.None, ExpectedETag);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValidFormat == expectedValid,
            $"{scenario}: expected IsValidFormat={expectedValid}, actual={result.IsValidFormat}.");
        Assert.Equal(bytes.Length, result.ContentLength);
        Assert.Equal(ExpectedETag, result.ETag);
        Assert.All(requestedETags, etag => Assert.Equal(ExpectedETag, etag));
        if (!expectedValid)
        {
            Assert.NotNull(result.FailureCode);
        }
    }

    [Theory]
    [InlineData("reserved.md", 5)]
    [InlineData("reserved.pdf", 5)]
    [InlineData("reserved.exe", 100)]
    public async Task OversizedOrUnsupportedUploadIsRejectedBeforeDownloading(string reference, long maximumSize)
    {
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal(HttpMethod.Head, request.Method);
            return CreateContentResponse(request, "%PDF- sample"u8.ToArray());
        });
        var result = await CreateStorage(httpClient).InspectUploadAsync(reference, maximumSize, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsValidFormat);
    }

    [Fact]
    public async Task MarkdownChangedBetweenPropertiesAndDownloadCannotBeVerified()
    {
        using var httpClient = CreateHttpClient(request => request.Method == HttpMethod.Head
            ? CreateContentResponse(request, "# Notes"u8.ToArray())
            : new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

        await Assert.ThrowsAsync<DocumentBlobChangedException>(() => CreateStorage(httpClient)
            .InspectUploadAsync("reserved.md", 100, CancellationToken.None, ExpectedETag));
    }

    [Theory]
    [InlineData("reserved.txt")]
    [InlineData("reserved.docx")]
    [InlineData("reserved.exe")]
    public async Task UnknownExtensionIsRejectedEvenWhenContentIsValidUtf8(string reference)
    {
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal(HttpMethod.Head, request.Method);
            return CreateContentResponse(request, "# Plain UTF-8 text"u8.ToArray());
        });

        var result = await CreateStorage(httpClient).InspectUploadAsync(reference, 100, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsValidFormat);
        Assert.Equal("unsupported_format", result.FailureCode);
    }

    [Theory]
    [InlineData("reserved.pdf", false)]
    [InlineData("reserved.md", false)]
    [InlineData("reserved.pdf", true)]
    [InlineData("reserved.md", true)]
    public async Task KnownFormatWithoutItsValidatorIsRejectedBeforeDownloading(string reference, bool registerOtherValidator)
    {
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal(HttpMethod.Head, request.Method);
            return CreateContentResponse(request, "%PDF- sample text"u8.ToArray());
        });
        IDocumentContentValidator[] validators = registerOtherValidator
            ? reference.EndsWith(".pdf", StringComparison.Ordinal)
                ? [new MarkdownDocumentContentValidator()]
                : [new PdfDocumentContentValidator()]
            : [];

        var result = await CreateStorage(httpClient, validators).InspectUploadAsync(reference, 100, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsValidFormat);
        Assert.Equal("unsupported_format", result.FailureCode);
    }

    [Theory]
    [InlineData("reserved.pdf", "bytes=0-4")]
    [InlineData("reserved.md", "bytes=0-16")]
    public async Task ValidatorControlsTheRequiredDownloadRange(string reference, string expectedRange)
    {
        using var httpClient = CreateHttpClient(request =>
        {
            if (request.Method != HttpMethod.Head)
            {
                Assert.Equal(expectedRange, Assert.Single(request.Headers.GetValues("x-ms-range")));
            }
            return CreateContentResponse(request, "%PDF- sample text"u8.ToArray());
        });

        var result = await CreateStorage(httpClient).InspectUploadAsync(reference, 100, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsValidFormat);
    }

    [Fact]
    public async Task CleanupReadsOnlyPropertiesEvenForMalformedMarkdown()
    {
        using var httpClient = CreateHttpClient(request =>
        {
            Assert.Equal(HttpMethod.Head, request.Method);
            return CreateContentResponse(request, new byte[] { 0xFF, 0 });
        });

        Assert.Equal(ExpectedETag, await CreateStorage(httpClient).GetUploadETagAsync("reserved.md", CancellationToken.None));
    }

    private static HttpResponseMessage CreateContentResponse(HttpRequestMessage request, byte[] bytes)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Headers.ETag = new(ExpectedETag);
        response.Headers.Add("x-ms-blob-type", "BlockBlob");
        response.Headers.Add("x-ms-request-id", "test");
        response.Content.Headers.ContentLength = bytes.Length;
        response.Content.Headers.LastModified = DateTimeOffset.UtcNow;
        if (request.Method != HttpMethod.Head)
        {
            response.StatusCode = HttpStatusCode.PartialContent;
            response.Content.Headers.ContentRange = new(0, bytes.Length - 1, bytes.Length);
        }
        return response;
    }

    private static AzureBlobDocumentStorage CreateStorage(
        HttpClient httpClient, IEnumerable<IDocumentContentValidator>? validators = null)
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
            serviceClient.GetBlobContainerClient(ContainerName),
            validators ?? [new PdfDocumentContentValidator(), new MarkdownDocumentContentValidator()]);
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
