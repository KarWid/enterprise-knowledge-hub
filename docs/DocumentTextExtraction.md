# Document text extraction (Milestone 8.1)

Markdown (`.md`) uploads now produce durable extracted text in Azure SQL.
PDF uploads still verify, but PDF extraction is disabled and no PDF extractor is
registered. Markdown [chunking](DocumentChunking.md) is a separate durable stage;
embeddings and retrieval remain future slices. The agreed MVP retrieval backend
is Azure SQL with chunk-owned vectors, replacing the earlier
Azure AI Search plan.

## Processing and ownership

Successful upload verification stages Pending extraction work in the same EF
transaction as `CompleteUpload`. A separate BackgroundService in the existing API
claims that work and periodically backfills bounded batches of verified Uploaded
documents without extraction records. Historical uploads without `ValidatedETag`
remain blocked; the worker never manufactures validation evidence.

`knowledge.DocumentTextExtractions` stores OrganizationId, DocumentId, SourceETag,
format/extractor version, execution state, attempts, lease metadata, stable error
code, and completed text. A unique source-identity index deduplicates scheduling;
a composite foreign key enforces Document ownership. Text uses `nvarchar(max)`.
Claims, lists, and status polling project metadata only.

Claims are atomic conditional updates with expiring leases. Attempts count claims,
including crash recovery; processing times out before lease expiry. Only an
unexpired owner can complete, fail, or retry. Completion atomically saves the text,
character count, and Completed state while checking current Document ownership,
Uploaded status, and validated ETag. Storage reads occur outside database transactions.

The processor validates the persisted canonical blob reference and uses
`If-Match: SourceETag` for properties and content. Missing/changed source bytes
fail permanently; they are never silently accepted as replacements. Storage
authorization/network/container failures retry with bounded exponential backoff.
Exhausted retries or crash recoveries become DeadLetter.

`Document.Status` remains Uploaded even when extraction fails. Extraction never
schedules original-file cleanup. Completed extraction does not mean Ready for
knowledge retrieval. A stopped API cannot process work, but work survives restarts;
preserve Always On in Azure.

## Markdown behavior and security

The registered `IDocumentTextExtractor` uses strict UTF-8 decoding, removes an
optional leading BOM, and normalizes CRLF/CR to LF. Markdown syntax, Unicode,
headings, tables, and code fences are preserved. Empty text, invalid UTF-8, and
binary controls other than tab/CR/LF fail with stable codes. Input/output limits
bound memory and storage; failures never persist partial text. Interrupted reads
retry, and cancellation is supported.

The extractor does not render HTML, execute code, fetch links, or call an LLM.
Text remains untrusted. Verification/extraction do not imply malware clearance or
prompt-injection protection. Any future required scan must add durable accepted-clean
evidence tied to the exact validated ETag to `DocumentProcessingGate`; missing,
error, timeout, and non-clean outcomes must remain blocked.

To support another format, implement/register a versioned `IDocumentTextExtractor`,
review its gates, add behavior tests, and enable its flag. A new upload extension
also needs a catalog entry and byte validator. Unknown formats have no UTF-8
fallback. Jobs with unavailable extractor versions fail explicitly; upgrading an
extractor requires a reviewed reprocessing strategy.

## Configuration

`appsettings.json` lists concrete defaults matching code. Empty placeholders are
retained for required external endpoints/identity configuration; typed booleans,
numbers, and format flags use actual values. Both upload formats are enabled.

```json
"DocumentTextExtraction": {
  "EnabledFormats": { "pdf": false, "markdown": true },
  "MaxSourceSizeBytes": 26214400,
  "MaxTextCharacters": 5000000
},
"DocumentTextExtractionWorker": {
  "Enabled": true,
  "PollSeconds": 60,
  "BatchSize": 10,
  "MaxAttempts": 8,
  "RetryBaseSeconds": 10,
  "RetryMaxSeconds": 600,
  "LeaseSeconds": 120,
  "ProcessingTimeoutSeconds": 90
}
```

Upload flags affect new reservations only. Extraction format flags independently
control scheduling and claiming. For example,
`DocumentTextExtraction__EnabledFormats__markdown=false` pauses pending work
without consuming retries; completed text stays available. Re-enable to resume
and discover verified uploads made while disabled. Omitted settings retain code
defaults. Unknown identifiers, invalid limits, and enabled formats without a
registered extractor fail options validation. Do not enable PDF extraction yet.

`DocumentTextExtractionWorker__Enabled=false` stops execution while upload
verification can still stage work. `DocumentUploadWorker.Enabled` is now explicitly
true in appsettings, matching its code default. Restart all API instances after
configuration changes; they do not immediately cancel work in older processes.

## API and UI

Document list/status responses include `textExtraction` metadata (status, character
count, successful completion time, and stable error code). Execution states are
Pending, Processing, Completed, Failed, DeadLetter. Derived availability is
NotAvailable before verified upload, Disabled for a disabled format without work,
or Paused when the format/worker is disabled and extraction is pending.

`GET /api/documents/{documentId}/text-preview` returns up to 4,000 UTF-16 code units
without a trailing split surrogate, full character count, truncation flag, and
completion time. SQL projects only that bounded substring. Unavailable/inaccessible
results return 404. `IDocumentDiagnosticsAccessService` also gates the preview:
access requires `DocumentDiagnostics:Enabled` (currently `true` in `appsettings.json`,
`false` if absent) and the `Documents.Diagnostics` policy, which requires an
authenticated `OrganizationOwner` in the validated current Organization.
Denied diagnostics access returns 404 for text and chunk previews without stopping
extraction/chunking or hiding basic extraction status. Both endpoints require Documents.Read and validated membership;
queries join/filter by Organization, Document, and matching source ETag. They never
expose storage references, SAS values, or lease internals.

React separates query caches by selected Organization, renders previews as escaped
text, and polls every 15 seconds for up to 90 seconds after page load/upload/Refresh. Poll expiry
only stops browser refresh; durable processing continues. Manual Refresh restarts
the window. Do not interpret source text as Markdown/HTML in the preview.

## Migration and verification

Review/apply `20261108092245_AddDocumentTextExtraction` through the normal migration
process before starting the updated API. It adds the extraction table, indexes,
and composite Document ownership key. This change does not apply migrations to
configured development or Azure databases automatically.

```powershell
dotnet ef database update --project src/Modules/Knowledge/EnterpriseKnowledgeHub.Modules.Knowledge --startup-project src/Api/EnterpriseKnowledgeHub.Api --context KnowledgeDbContext
dotnet build EnterpriseKnowledgeHub.slnx
dotnet test EnterpriseKnowledgeHub.slnx --no-build
npm --prefix src/Web run build
```

Integration tests normally use SQLite. Set `EKH_TEST_SQLSERVER=1` to run against
isolated, uniquely named LocalDB databases that the fixture creates and deletes:

```powershell
$env:EKH_TEST_SQLSERVER = '1'
dotnet test tests/Integration/EnterpriseKnowledgeHub.IntegrationTests --filter 'FullyQualifiedName~DocumentTextExtractionTests'
Remove-Item Env:EKH_TEST_SQLSERVER
```

For a manual check, upload Markdown using the existing upload setup; expect
Uploaded, then Text extracted and a plain preview preserving headings/code.
PDF stays Uploaded with extraction disabled. Restart the API with pending work
and exercise disable/re-enable to verify eventual completion without reuploading.

## Recovery

Monitor Failed/DeadLetter records and worker error codes. Logs contain identifiers
and codes, never document text or raw Storage exception messages. After resolving
a dependency failure, an authorized operator may redrive one reviewed DeadLetter
row with parameters identifying its exact Organization and source:

```sql
UPDATE extraction
SET Status = 'Pending', Attempts = 0, NextAttemptAt = SYSUTCDATETIME(),
    LeaseOwner = NULL, LeaseExpiresAt = NULL, CompletedAt = NULL, LastError = NULL
FROM knowledge.DocumentTextExtractions AS extraction
JOIN knowledge.Documents AS document
  ON document.OrganizationId = extraction.OrganizationId
 AND document.Id = extraction.DocumentId
 AND document.ValidatedETag = extraction.SourceETag
WHERE extraction.Id = @ReviewedExtractionId
  AND extraction.OrganizationId = @ReviewedOrganizationId
  AND extraction.Status = 'DeadLetter'
  AND document.Status = 'Uploaded';
```

Failed content/source work needs a reviewed fix or a new upload. Never replace a
job's ETag with the current blob ETag to bypass validation. Keep completed results
and source deduplication identities until a separate retention policy is designed.
