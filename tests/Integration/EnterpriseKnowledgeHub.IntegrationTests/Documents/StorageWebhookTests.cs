using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class StorageWebhookTests
{
    private const string WebhookPath = "/webhooks/storage-events";
    private const string ValidationCode = "validation-code";

    [Theory]
    [InlineData(UnauthorizedRequestScenario.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(UnauthorizedRequestScenario.InvalidSignature, HttpStatusCode.Unauthorized)]
    [InlineData(UnauthorizedRequestScenario.ExpiredToken, HttpStatusCode.Unauthorized)]
    [InlineData(UnauthorizedRequestScenario.InvalidAudience, HttpStatusCode.Unauthorized)]
    [InlineData(UnauthorizedRequestScenario.InvalidIssuer, HttpStatusCode.Unauthorized)]
    [InlineData(UnauthorizedRequestScenario.InvalidSender, HttpStatusCode.Forbidden)]
    [InlineData(UnauthorizedRequestScenario.MissingRole, HttpStatusCode.Forbidden)]
    [InlineData(UnauthorizedRequestScenario.InteractiveUser, HttpStatusCode.Forbidden)]
    public async Task UnauthorizedRequest_DoesNotEnqueueWork(
        UnauthorizedRequestScenario scenario,
        HttpStatusCode expectedStatusCode)
    {
        // Arrange
        using var factory = new StorageWebhookFactory();
        using var client = factory.CreateClient();
        var token = CreateTokenForScenario(factory, scenario);

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // An Event Grid header is metadata and must never be accepted as authentication.
        client.DefaultRequestHeaders.Add("aeg-event-type", "SubscriptionValidation");
        var events = new[]
        {
            CreateBlobCreatedEvent()
        };

        // Act
        using var response = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        await using var assertDb = factory.Database.CreateContext();
        var inboxItems = await assertDb.UploadInbox.AsNoTracking().ToListAsync();
        Assert.Empty(inboxItems);
    }

    [Fact]
    public async Task DisabledWebhook_WithValidToken_RemainsClosed()
    {
        // Arrange
        using var factory = new StorageWebhookFactory
        {
            Enabled = false
        };
        using var client = CreateAuthenticatedClient(factory);
        var events = new[]
        {
            CreateBlobCreatedEvent()
        };

        // Act
        using var response = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubscriptionValidation_WithValidAuthentication_ReturnsCodeWithoutEnqueueingWork()
    {
        // Arrange
        using var factory = new StorageWebhookFactory();
        using var client = CreateAuthenticatedClient(factory);
        var events = new[]
        {
            CreateSubscriptionValidationEvent()
        };

        // Act
        using var response = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        var returnedValidationCode = responseBody.GetProperty("validationResponse").GetString();
        Assert.Equal(ValidationCode, returnedValidationCode);

        await using var assertDb = factory.Database.CreateContext();
        var inboxItems = await assertDb.UploadInbox.AsNoTracking().ToListAsync();
        Assert.Empty(inboxItems);
    }

    [Fact]
    public async Task ValidBatch_WhenDeliveredTwice_IsAcknowledgedWithoutCreatingDuplicateWork()
    {
        // Arrange
        using var factory = new StorageWebhookFactory();
        using var client = CreateAuthenticatedClient(factory);
        var events = new[]
        {
            CreateBlobCreatedEvent(),
            CreateBlobCreatedEvent()
        };

        // Act
        using var firstDeliveryResponse = await client.PostAsJsonAsync(WebhookPath, events);
        using var duplicateDeliveryResponse = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstDeliveryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicateDeliveryResponse.StatusCode);

        await using var assertDb = factory.Database.CreateContext();
        var inboxItems = await assertDb.UploadInbox.AsNoTracking().ToListAsync();

        Assert.True(
            inboxItems.Count == events.Length,
            "Delivering the same Event Grid batch twice must not create duplicate inbox work.");
        Assert.All(inboxItems, item =>
        {
            Assert.Equal(UploadWorkStatus.Pending, item.Status);
        });
    }

    [Theory]
    [InlineData(StorageEventFault.UnexpectedSource)]
    [InlineData(StorageEventFault.UnexpectedContainer)]
    [InlineData(StorageEventFault.UntrustedUrl)]
    [InlineData(StorageEventFault.InvalidBlobReference)]
    [InlineData(StorageEventFault.UnexpectedEventType)]
    [InlineData(StorageEventFault.UnsupportedStorageApi)]
    [InlineData(StorageEventFault.UnsupportedBlobType)]
    [InlineData(StorageEventFault.InvalidETag)]
    public async Task InvalidStorageEvent_IsRejectedWithoutEnqueueingWork(StorageEventFault fault)
    {
        // Arrange
        using var factory = new StorageWebhookFactory();
        using var client = CreateAuthenticatedClient(factory);
        var events = new[]
        {
            CreateBlobCreatedEvent(fault)
        };

        // Act
        using var response = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var assertDb = factory.Database.CreateContext();
        var inboxItems = await assertDb.UploadInbox.AsNoTracking().ToListAsync();
        Assert.Empty(inboxItems);
    }

    [Fact]
    public async Task ValidEvent_WhenInboxPersistenceFails_ReturnsRetryableStatus()
    {
        // Arrange
        using var factory = new StorageWebhookFactory
        {
            SimulateInboxPersistenceFailure = true
        };
        using var client = CreateAuthenticatedClient(factory);
        var events = new[]
        {
            CreateBlobCreatedEvent()
        };

        // Act
        using var response = await client.PostAsJsonAsync(WebhookPath, events);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static HttpClient CreateAuthenticatedClient(StorageWebhookFactory factory)
    {
        var client = factory.CreateClient();
        var token = factory.Token();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string? CreateTokenForScenario(
        StorageWebhookFactory factory,
        UnauthorizedRequestScenario scenario)
    {
        return scenario switch
        {
            UnauthorizedRequestScenario.Anonymous => null,
            UnauthorizedRequestScenario.InvalidSignature => factory.Token(wrongSignature: true),
            UnauthorizedRequestScenario.ExpiredToken => factory.Token(expired: true),
            UnauthorizedRequestScenario.InvalidAudience => factory.Token(audience: "different-api"),
            UnauthorizedRequestScenario.InvalidIssuer => factory.Token(issuer: "https://untrusted.example"),
            UnauthorizedRequestScenario.InvalidSender => factory.Token(sender: Guid.NewGuid().ToString()),
            UnauthorizedRequestScenario.MissingRole => factory.Token(role: null),
            UnauthorizedRequestScenario.InteractiveUser => factory.Token(user: true),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static object CreateSubscriptionValidationEvent()
    {
        return new
        {
            topic = StorageWebhookFactory.Source,
            eventType = "Microsoft.EventGrid.SubscriptionValidationEvent",
            data = new
            {
                validationCode = ValidationCode
            }
        };
    }

    private static object CreateBlobCreatedEvent(StorageEventFault? fault = null)
    {
        var reference = fault == StorageEventFault.InvalidBlobReference
            ? "../other.pdf"
            : $"organizations/{Guid.NewGuid():N}/documents/{Guid.NewGuid():N}.pdf";
        var containerName = fault == StorageEventFault.UnexpectedContainer
            ? "other"
            : "documents";
        var blobUrl = fault == StorageEventFault.UntrustedUrl
            ? "https://untrusted.example/blob"
            : $"https://teststore.blob.core.windows.net/documents/{reference}";

        return new
        {
            id = Guid.NewGuid().ToString(),
            topic = fault == StorageEventFault.UnexpectedSource
                ? "different-source"
                : StorageWebhookFactory.Source,
            subject = $"/blobServices/default/containers/{containerName}/blobs/{reference}",
            eventType = fault == StorageEventFault.UnexpectedEventType
                ? "Microsoft.Storage.BlobDeleted"
                : "Microsoft.Storage.BlobCreated",
            data = new
            {
                api = fault == StorageEventFault.UnsupportedStorageApi
                    ? "PutBlockList"
                    : "PutBlob",
                blobType = fault == StorageEventFault.UnsupportedBlobType
                    ? "AppendBlob"
                    : "BlockBlob",
                eTag = fault == StorageEventFault.InvalidETag
                    ? "invalid"
                    : "0x123",
                url = blobUrl
            }
        };
    }

    public enum UnauthorizedRequestScenario
    {
        Anonymous,
        InvalidSignature,
        ExpiredToken,
        InvalidAudience,
        InvalidIssuer,
        InvalidSender,
        MissingRole,
        InteractiveUser
    }

    public enum StorageEventFault
    {
        UnexpectedSource,
        UnexpectedContainer,
        UntrustedUrl,
        InvalidBlobReference,
        UnexpectedEventType,
        UnsupportedStorageApi,
        UnsupportedBlobType,
        InvalidETag
    }
}
