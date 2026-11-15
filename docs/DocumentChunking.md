# Document chunking (Milestone 8.2)

Completed Markdown extractions now produce durable, Organization-owned chunks in
the existing Knowledge SQL database. Original blobs and full extracted text remain
unchanged. Embeddings and retrieval remain separate, future stages; a completed
chunk set does not make a Document Ready.

## Processing and persistence

A BackgroundService in the existing API discovers bounded batches of completed
extractions whose verified Document and source ETag still match. Discovery reads
metadata only and survives missed notifications or application restarts. It does
not require a blob read or an ambient request Organization.

`knowledge.DocumentChunkings` stores the extraction identity, Organization,
Document, source ETag, format, chunker version, effective settings, configuration
key, execution status, attempts, expiring lease, stable error code, and chunk count.
The unique `(ExtractionId, ConfigurationKey)` index deduplicates scheduling.
`knowledge.DocumentChunks` stores ordered content, heading path, and token count.
Composite foreign keys enforce ownership from extraction to operation to chunks;
`(ChunkingId, Ordinal)` is unique.

Claims are conditional updates. Only an unexpired lease owner can publish a set,
fail, or retry. Completion rechecks the current Document and completed extraction,
then inserts all chunks and marks the operation Completed in one transaction.
Failures publish no partial set. Transient errors use bounded exponential backoff;
exhausted retries or crash recoveries become DeadLetter. Permanent content/version
failures become Failed and are not automatically redriven. Document upload status
and original-file cleanup are unaffected.

## Markdown behavior

The registered `IDocumentChunker` is versioned `markdown-structure-cl100k-v1`.
Markdig parses Markdown blocks without rendering HTML, executing code, fetching
links/images, or calling an LLM. Headings (including Setext syntax) establish a
hierarchy. Adjacent blocks in a section are grouped near the target token count;
a single complete block may exceed the target while staying under the maximum.
Headings finish the previous section, and heading-like text inside code remains
content. Heading-only documents still produce chunks. Plain text needs no headings.

Paragraph splitting prefers sentence boundaries, then whitespace, then a bounded
Unicode-safe token split. Small lists, tables, and code blocks remain intact.
Oversized lists split by item, oversized code blocks repeat opening/closing fences,
and oversized pipe tables repeat column headers. Very large individual lines,
rows, or headers fall back to bounded text pieces; perfect syntax preservation
cannot be guaranteed for an element that exceeds the entire budget.

Each Content includes document-name/section context. Context is bounded, and token
counts include it. Heading metadata is bounded separately. Optional overlap uses
trailing prose within the same section; it does not cross headings or repeat code
and tables as overlap. Counts use the packaged `cl100k_base` vocabulary through
Microsoft.ML.Tokenizers; no runtime vocabulary download is needed. A future
embedding stage must use a compatible tokenizer or introduce a new chunker version.
Input/output bounds and cancellation prevent unbounded result production. Chunked
content remains untrusted data, including HTML and embedded prompt instructions.

## Configuration

```json
"DocumentChunking": {
  "EnabledFormats": { "pdf": false, "markdown": true },
  "TargetTokens": 500,
  "MaxTokens": 800,
  "OverlapTokens": 64,
  "MaxChunksPerDocument": 10000,
  "MaxSourceCharacters": 5000000
},
"DocumentChunkingWorker": {
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

Format flags use the same catalog identifiers as extraction: `markdown` maps to
`.md`, `pdf` to `.pdf`. Unknown identifiers, invalid bounds, and enabled formats
without a registered, versioned chunker fail startup validation, even when the
worker is disabled. Duplicate chunker registrations also fail. No PDF chunker is
registered; leave its flag false. Adding PDF extraction alone does not enable PDF
chunking.

Upload, extraction, and chunking flags are independent. Disabling Markdown
chunking pauses scheduling/claiming without consuming attempts or deleting prior
results. Disabling extraction does not prevent chunking already completed text.
`DocumentChunkingWorker__Enabled=false` stops the worker; discovery backfills on
restart. A stopped API cannot process work. Configuration requires restarting all
API instances and does not cancel work already running in another process.

The configuration key includes format, chunker version, and effective settings.
Changing settings creates a new set; API status/previews select the current
configuration. Old sets remain stored, and pending work retains its original
settings. Unavailable chunker versions fail explicitly. Review processing volume
before changing versions/settings: existing completed extractions are backfilled
for the new configuration, and an incomplete new set does not fall back to an old
set. Retention and operator redrive remain explicit maintenance concerns; never
edit source ownership or ETags to bypass validation.

Diagnostics availability is configured separately from processing:

```json
"DocumentDiagnostics": {
  "Enabled": true
}
```

Set `DocumentDiagnostics__Enabled=false` in environment configuration to disable
previews and chunking metadata. Restart the API after changing this setting. The
current implementation uses standard .NET configuration options; it does not yet
connect to Azure App Configuration or its feature-management SDK.

## API and UI

The shared `SelectDocumentResults` projection has two overloads: the basic view
loads document and current-source extraction metadata; the full view additionally
loads current-configuration chunking metadata. Both document handlers select the
projection and result mapping using `IDocumentDiagnosticsAccessService`. The text
and chunk preview handlers await the same service. Its API implementation requires
both `DocumentDiagnostics:Enabled` and successful `Documents.Diagnostics` policy
authorization. The policy requires authentication and the `OrganizationOwner` role
on the validated current Organization membership. Owner claims or ownership of a
different Organization do not grant access. Existing Documents.Read and Organization
validation still apply. Knowledge depends only on the asynchronous access-service
interface; ASP.NET authorization and HTTP context remain in the API.
The flag is currently `true` in `appsettings.json` for development and defaults to
`false` when absent. When diagnostics access is denied, document handlers return
the basic view and preview handlers return 404.
Each view uses one SQL query and neither loads source
or chunk text. Status resolvers calculate availability after materialization
without database access.

The full view's `chunking` result includes status, chunk count, successful completion
time, and stable failure code. Execution states are Pending, Processing, Completed,
Failed, DeadLetter. Derived availability includes NotAvailable, Disabled, and Paused.

`GET /api/documents/{documentId}/chunks?offset=0&limit=5` returns ordered previews
from the current completed set. Limit is 1–20 and offset must be nonnegative.
The query constructor validates these bounds for both HTTP and internal callers.
Out-of-range HTTP pagination returns 400 through the existing exception middleware,
with code `invalid_request` and a message identifying the invalid parameter.
Invalid pagination is not represented by a null result or a 404 response.
SQL bounds each content preview to 4,000 UTF-16 units (plus one truncation probe);
responses do not end with a split surrogate. Responses include truncation flags,
total chunk count, and pagination metadata. Authentication, Documents.Read policy,
and validated Organization membership are required. Ownership/current-source
filters are checked when reading content. Successful extraction is verified when
the chunk set is completed, so preview queries do not recheck the extraction table.
Inaccessible or unavailable sets return
404. When the diagnostics access service denies access, both the chunk and extracted-text
preview handlers return 404 without querying preview data. Extraction and chunking
continue independently of diagnostics availability. No storage references, source
ETags, lease details, or configuration keys cross the API boundary.

React shows one colored status icon per document. Hovering or focusing it reveals
the upload, extraction, and chunking descriptions; missing or disabled processing
stages are omitted. Errors take precedence over paused or active work when choosing
the indicator color.
A vertical three-dot menu offers content preview after successful extraction and
chunk preview after successful chunking. Both actions require the optional chunking
metadata that identifies the current full diagnostics view. The basic view omits
these actions. The endpoints retain their existing Documents.Read policy; the
shared service additionally evaluates Documents.Diagnostics. Previews render escaped
text. Query caches include the selected Organization, and content is never interpreted as
Markdown/HTML. Document list and upload-status polling runs every 15 seconds for
up to 90 seconds. Manual Refresh restarts the list polling window. Processing can
finish after browser polling stops; refresh to check progress.

## Migration and verification

Apply `20261114214320_AddDocumentChunking` through the normal migration process
before starting the updated API. This adds two tables and the extraction ownership
key. Implementation and tests do not migrate a configured development/Azure database.

```powershell
dotnet ef database update --project src/Modules/Knowledge/EnterpriseKnowledgeHub.Modules.Knowledge --startup-project src/Api/EnterpriseKnowledgeHub.Api --context KnowledgeDbContext
dotnet build EnterpriseKnowledgeHub.slnx
dotnet test EnterpriseKnowledgeHub.slnx --no-build
npm --prefix src/Web run build
```

Integration tests cover Markdown structure/plain-text fallback, token limits,
configuration validation, recovery, transactions, ownership, and protected API
behavior. As with extraction, `EKH_TEST_SQLSERVER=1` enables isolated LocalDB
databases for SQL Server migration/persistence checks. No Azure resources are added.
