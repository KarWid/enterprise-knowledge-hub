using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseKnowledgeHub.Infrastructure.BlobStorage;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Api.StorageEvents;

public sealed record ParsedStorageEvents(string? ValidationCode, IReadOnlyList<UploadInboxItem> Items);

public sealed partial class StorageEventParser(
    IOptions<StorageEventOptions> settings, IOptions<BlobStorageOptions> storage, TimeProvider clock)
{
    private const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";
    private const string BlobCreatedEvent = "Microsoft.Storage.BlobCreated";
    private const string PutBlobOperation = "PutBlob";
    private const string BlockBlobType = "BlockBlob";

    public ParsedStorageEvents Parse(JsonElement batch)
    {
        var batchSize = ValidateBatch(batch);
        var items = new List<UploadInboxItem>();

        foreach (var eventJson in batch.EnumerateArray())
        {
            var source = ValidateSource(eventJson);
            var eventType = RequiredString(eventJson, "eventType");
            var eventData = RequiredObject(eventJson, "data");

            if (eventType == SubscriptionValidationEvent)
            {
                return ParseSubscriptionValidation(eventData, batchSize);
            }

            items.Add(ParseBlobCreated(eventJson, eventData, eventType, source));
        }

        return new(null, items);
    }

    private int ValidateBatch(JsonElement batch)
    {
        if (batch.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Invalid Event Grid batch.");
        }

        var batchSize = batch.GetArrayLength();
        if (batchSize is < 1 || batchSize > settings.Value.MaximumBatchSize)
        {
            throw new ArgumentException("Invalid Event Grid batch.");
        }

        return batchSize;
    }

    private string ValidateSource(JsonElement eventJson)
    {
        var source = RequiredString(eventJson, "topic");
        if (!string.Equals(source, settings.Value.ExpectedSource, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unexpected event source.");
        }

        return source;
    }

    private static ParsedStorageEvents ParseSubscriptionValidation(JsonElement eventData, int batchSize)
    {
        var validationCode = RequiredString(eventData, "validationCode");
        if (batchSize != 1 || validationCode.Length is < 1 or > 256)
        {
            throw new ArgumentException("Invalid subscription validation.");
        }

        return new(validationCode, []);
    }

    private UploadInboxItem ParseBlobCreated(
        JsonElement eventJson,
        JsonElement eventData,
        string eventType,
        string source)
    {
        ValidateBlobCreatedEvent(eventData, eventType);

        var blobReference = ParseBlobReference(eventJson);
        ValidateStorageUrl(eventData, source, blobReference);

        var eventId = ParseEventId(eventJson);
        var etag = ParseETag(eventData);
        var createdAt = clock.GetUtcNow().UtcDateTime;

        return new UploadInboxItem
        {
            Source = settings.Value.ExpectedSource.ToLowerInvariant(),
            EventId = eventId,
            BlobReference = blobReference,
            ETag = $"\"{etag}\"",
            Kind = UploadWorkKind.Verify,
            CreatedAt = createdAt,
            NextAttemptAt = createdAt
        };
    }

    private static void ValidateBlobCreatedEvent(JsonElement eventData, string eventType)
    {
        if (eventType != BlobCreatedEvent ||
            RequiredString(eventData, "api") != PutBlobOperation ||
            RequiredString(eventData, "blobType") != BlockBlobType)
        {
            throw new ArgumentException("Unsupported storage event.");
        }
    }

    private string ParseBlobReference(JsonElement eventJson)
    {
        var expectedSubjectPrefix =
            $"/blobServices/default/containers/{storage.Value.ContainerName}/blobs/";
        var subject = RequiredString(eventJson, "subject");

        if (!subject.StartsWith(expectedSubjectPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Unexpected blob container.");
        }

        var blobReference = subject[expectedSubjectPrefix.Length..];
        if (!BlobReferencePattern().IsMatch(blobReference))
        {
            throw new ArgumentException("Unexpected blob reference.");
        }

        return blobReference;
    }

    private void ValidateStorageUrl(JsonElement eventData, string source, string blobReference)
    {
        var configuredAccount = ParseConfiguredStorageAccount();
        var sourceAccountName = source.Split('/').Last();

        if (!string.Equals(
                configuredAccount.Host.Split('.')[0],
                sourceAccountName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unexpected storage URL.");
        }

        if (!Uri.TryCreate(RequiredString(eventData, "url"), UriKind.Absolute, out var eventUrl) ||
            !string.IsNullOrEmpty(eventUrl.UserInfo) ||
            !string.IsNullOrEmpty(eventUrl.Query) ||
            !string.IsNullOrEmpty(eventUrl.Fragment))
        {
            throw new ArgumentException("Unexpected storage URL.");
        }

        var expectedUrl = new Uri(configuredAccount, $"{storage.Value.ContainerName}/{blobReference}");

        if (eventUrl != expectedUrl)
        {
            throw new ArgumentException("Unexpected storage URL.");
        }
    }

    private Uri ParseConfiguredStorageAccount()
    {
        if (!Uri.TryCreate(storage.Value.AccountUrl.TrimEnd('/') + "/", UriKind.Absolute, out var account) ||
            account.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Unexpected storage URL.");
        }

        return account;
    }

    private static string ParseEventId(JsonElement eventJson)
    {
        if (!Guid.TryParse(RequiredString(eventJson, "id"), out var eventId))
        {
            throw new ArgumentException("Invalid event identity.");
        }

        return eventId.ToString("D");
    }

    private static string ParseETag(JsonElement eventData)
    {
        var etag = RequiredString(eventData, "eTag").Trim('"');
        if (!ETagPattern().IsMatch(etag))
        {
            throw new ArgumentException("Invalid event identity.");
        }

        return etag;
    }

    private static JsonElement RequiredObject(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Object
            ? value
            : throw new ArgumentException("Missing event field.");

    private static string RequiredString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new ArgumentException("Missing event field.");

    [GeneratedRegex("^organizations/[a-f0-9]{32}/documents/[a-f0-9]{32}\\.pdf$")]
    private static partial Regex BlobReferencePattern();
    
    [GeneratedRegex("^0x[0-9a-fA-F]{1,64}$")]
    private static partial Regex ETagPattern();
}
