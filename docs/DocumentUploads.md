# Asynchronous document uploads

The API never proxies PDF bytes. It authorizes Documents.Upload using the current
Organization, constructs a unique reservation and a user delegation SAS, commits
the reservation, and only then returns the SAS. SAS generation happens before
the commit so a delegation-key failure leaves no unused row; no capability leaves
the API before its reservation is durable. Neither the database nor logs store SAS URLs.

React sends one `PUT Blob` with `If-None-Match: *`, `x-ms-blob-type: BlockBlob`,
`x-ms-blob-content-type: application/pdf`, and `x-ms-version: 2023-11-03`.
This operation supports create (`sp=c`) permission. Chunked SDK uploads using
Put Block/Put Block List are deliberately not used. The configured size limit
defaults to 25 MiB and is capped at 500 MiB for this single-request implementation.
The SAS covers one blob, HTTPS only, for 1–15 minutes; it grants no read, list,
write/overwrite, or delete permission. Azure uses DefaultAzureCredential and the
App Service managed identity; no account keys reach the browser.

BlobCreated → authenticated API webhook → SQL inbox → API BackgroundService →
conditional blob inspection → persisted document status. React polls
`GET /api/documents/{documentId}` every two seconds for up to two minutes and can
navigate away. Poll expiry or a status-query outage is not an upload rejection.
Returning to the list fetches persisted status again. `/complete` was removed:
repository consumers were the React hook, generated client/schema, and tests;
no other repository integration used it. External clients, if any, must migrate
to the status GET before this API version is rolled out.

## Status and consistency policy

- `PendingForUpload`: reservation exists but committed bytes have not passed verification.
- `Uploaded`: size and `%PDF-` header checks passed for `ValidatedETag`. This is
  neither full PDF validation nor proof that the file is malware-free.
- `Rejected`: invalid header, empty file, or size above the configured limit.
- `Failed`: the reserved blob was genuinely absent after deadline plus grace.
- Existing Processing/Ready/Archived meanings are unchanged; this milestone never
  schedules extraction or indexing and `DocumentProcessingGate.CanStartExtraction`
  remains false. Historical Uploaded rows have no validated ETag and are not
  automatically promoted through future gates.

The upload deadline is the original SAS expiry. The grace period defaults to
five minutes. Existing committed bytes are accepted even if processed much later.
Before the deadline plus grace, missing blobs retry. Afterwards, a genuinely
missing blob fails the reservation; Storage authorization, container-not-found,
network, and service failures do not count as missing blobs. A very late commit
after a reservation was already failed cannot reopen it and is cleaned up on its
notification. This bounds abandonment; the grace period should cover expected
upload completion/Storage delays for the supported size limit.

The webhook authenticates every delivery, including subscription validation. It
checks the configured source ARM ID, event type, PutBlob operation, account URL,
container, reference format, and ETag. It only persists normalized source/event
identity, blob reference, and ETag—never the payload or arbitrary event URL.
The worker looks up the exact reference and derives OrganizationId from that
reservation. Unmatched events complete harmlessly without touching Storage.
Stale ETags are ignored; deadline reconciliation verifies current committed bytes.
Reads use If-Match for properties and signature. No database transaction is open
during Storage operations. Document Revision is an optimistic concurrency token.

`knowledge.UploadInbox` has a unique `(Source, EventId)` index. Batch insertion
may partially commit; a failed batch returns 503 and replay deduplicates already
committed rows. Each claim is one conditional database UPDATE. Only the owner of
an unexpired lease can complete or retry work. Attempts count lease acquisitions,
including crashes. A processing timeout is shorter than the lease. Shutdown
cancels processing; abandoned leases recover automatically. There is one active
item per API instance, not one global worker.

Rejection/failure and cleanup scheduling share one EF transaction. Cleanup waits
until deadline plus grace, retaining the blob name while its create-only SAS can
still recreate it. It reads the current rejected blob and conditionally deletes
that ETag; changed content retries. Recreated blobs remain rejected and matching
later notifications schedule another cleanup. Successful deletion followed by a
worker crash is safe to repeat. Cleanup never touches Uploaded/Ready documents
or unmatched references. Keep inbox deduplication rows; do not purge them without
a separately designed retention/replay policy.

## Configuration

`StorageEvents` is disabled by default locally. Disabled means authentication
fails, not anonymous access. Enabled settings validate on startup:

| Setting | Value |
| --- | --- |
| `StorageEvents:Enabled` | `true` only with complete Entra setup |
| `TenantId` | Workforce tenant containing the webhook app, separate from frontend External ID |
| `Audience` | Webhook app's application/client GUID; configure v2 access tokens |
| `SenderApplicationId` | Microsoft.EventGrid application ID in that cloud/tenant |
| `RequiredRole` | `AzureEventGridSecureWebhookSubscriber` |
| `ExpectedSource` | Full storage account ARM resource ID, not a topic header used as authentication |
| `MaximumBatchSize` | 100; HTTP request limit is 1 MiB |

`DocumentUploadWorker` defaults are checked at startup. Enabled is false in local
appsettings and true in the Azure App Service template:

| Setting | Default |
| --- | --- |
| PollSeconds / BatchSize | 5 / 10 |
| MaxAttempts | 8 |
| RetryBaseSeconds / RetryMaxSeconds | 10 / 600 (bounded exponential backoff) |
| LeaseSeconds / ProcessingTimeoutSeconds | 120 / 90 (lease must exceed timeout by at least 15 s) |
| ReservationGraceSeconds | 300 |

No generic workflow engine, outbox, Service Bus, Functions, or separate deployment
is introduced. The inbox also carries reconciliation and cleanup work. Future
downstream scheduling must share the Document transaction; do not publish an
external job between a document update and SaveChanges.

## Local verification (repository root, PowerShell)

No Azure account or paid service is required for the default tests:

```powershell
dotnet restore EnterpriseKnowledgeHub.slnx -m:1 /nr:false
dotnet build EnterpriseKnowledgeHub.slnx --no-restore -m:1 /nr:false
dotnet test tests/Unit/EnterpriseKnowledgeHub.UnitTests/EnterpriseKnowledgeHub.UnitTests.csproj --no-build --no-restore
dotnet test tests/Integration/EnterpriseKnowledgeHub.IntegrationTests/EnterpriseKnowledgeHub.IntegrationTests.csproj --no-build --no-restore
Push-Location src/Web
npm ci
npm run api:generate
npm run build
Pop-Location
```

The portable integration harness uses SQLite, multiple connections, the real JWT
middleware with an in-process RSA issuer, and controllable Storage/time adapters.
These signing keys exist only in the test project; the API contains no local
machine-authentication bypass or event simulator route. It tests authentication,
the handshake, durability, uniqueness, claims/recovery, retry exhaustion,
reconciliation, and document races. Storage adapter tests check conditional reads
and SAS scope without contacting Azure.

For the same queue/finalization tests plus real migration execution against SQL
Server, install SQL Server Express LocalDB on Windows and run:

```powershell
$env:EKH_TEST_SQLSERVER = '1'
dotnet test tests/Integration/EnterpriseKnowledgeHub.IntegrationTests/EnterpriseKnowledgeHub.IntegrationTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Documents.UploadInboxTests|FullyQualifiedName~Documents.UploadFinalizationTests|FullyQualifiedName~Documents.DocumentUploadWorkerTests'
Remove-Item Env:EKH_TEST_SQLSERVER
```

This mode exclusively creates/migrates/drops randomly named `EKHUploadTests_*`
databases on `(localdb)\MSSQLLocalDB`; it accepts no remote connection string.

For a real Storage protocol test, start Azurite in another terminal:

```powershell
npx --yes --package azurite azurite-blob --silent --inMemoryPersistence --blobHost 127.0.0.1 --blobPort 10000 --skipApiVersionCheck
```

Then run the test harness/event simulator:

```powershell
$env:EKH_TEST_AZURITE = '1'
dotnet test tests/Integration/EnterpriseKnowledgeHub.IntegrationTests/EnterpriseKnowledgeHub.IntegrationTests.csproj --no-build --no-restore --filter FullyQualifiedName~AzuriteUploadTests
Remove-Item Env:EKH_TEST_AZURITE
```

The test creates an isolated private container, reserves a document, sends the
same single PUT headers/body shape as React with a create-only emulator SAS,
proves overwrite/read/delete are denied, synthesizes a BlobCreated batch through
the authenticated webhook, claims it, and finalizes using the real Azure SDK
adapter against Azurite. It removes its container in finally. Stop Azurite with
Ctrl+C afterwards. Azurite has no Event Grid delivery and this test does not
prove Entra/user-delegation behavior in Azure. Only this test uses the public
Azurite development key and HTTP service SAS; production has no key/SAS fallback.
It is a protocol harness, not a real-browser/CORS or live Azure integration test.

## Manual React/API test with an existing development Storage account

Azurite alone cannot exercise the production reservation endpoint's user
delegation SAS; use the isolated harness above for completely offline tests.
For the real local UI, keep your existing Entra API user secrets and React
`.env.local` values (see `src/Web/.env.example`). Sign in with an
OrganizationOwner, because the current Documents.Upload policy is owner-only.
Your signed-in Azure CLI developer identity needs Storage Blob Data Contributor
on the development storage account; its Blob CORS must allow `http://localhost:5173`
and the configured PUT headers, including Content-Type.

Stop any existing API debug session before building or running migrations.
In an API terminal at the repository root (replace only the account URL):

```powershell
az login
$apiProject = 'src/Api/EnterpriseKnowledgeHub.Api'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EnterpriseKnowledgeHubDbConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=EnterpriseKnowledgeHub;Integrated Security=true;TrustServerCertificate=true'
$env:BlobStorage__AccountUrl = 'https://<your-development-account>.blob.core.windows.net'
$env:BlobStorage__ContainerName = 'documents'
$env:DocumentUploadWorker__Enabled = 'true'
$env:StorageEvents__Enabled = 'false'
$env:Documents__UploadSasLifetimeMinutes = '1'
$env:DocumentUploadWorker__ReservationGraceSeconds = '60'

dotnet ef database update --project src/Modules/Identity/EnterpriseKnowledgeHub.Modules.Identity --startup-project $apiProject --context IdentityDbContext
dotnet ef database update --project src/Modules/Organizations/EnterpriseKnowledgeHub.Modules.Organizations --startup-project $apiProject --context OrganizationsDbContext
dotnet ef database update --project src/Modules/Knowledge/EnterpriseKnowledgeHub.Modules.Knowledge --startup-project $apiProject --context KnowledgeDbContext
dotnet run --project $apiProject --launch-profile https
```

These commands explicitly target LocalDB, not Azure SQL. A fresh database also
needs the repository's normal authorized-user/organization onboarding; it does
not import memberships from another database. If you already have a local
development database, use its local connection string instead. The temporary
environment settings apply only to this terminal. Use a trusted local HTTPS
development certificate for `https://localhost:7189`.

In a second terminal, run `npm run dev` from `src/Web` and open
`http://localhost:5173`. With `VITE_API_URL` unset, the existing Vite proxy forwards
`/api` to `https://localhost:7189`. Select your Organization and:

1. Upload a small PDF. Expect “Uploaded—verifying…” immediately after PUT.
2. With events disabled, the worker reconciles after the original one-minute
   SAS lifetime plus the 60-second grace. It will normally finish in about two
   minutes from reservation, plus a polling interval. The UI's two-minute polling
   window may end just before that; refresh the document list to see Uploaded.
3. Upload a text file renamed `.pdf`; after reconciliation it becomes Rejected,
   and its blob is conditionally removed. Do not use real malicious files.
4. Navigate away after a successful PUT, or stop/restart the API. The reserved
   document remains and verification completes independently of the browser.

This manual mode tests direct uploads and reconciliation. The Azurite/JWT test
harness separately tests the webhook/inbox path; real Event Grid delivery needs
the deployed authenticated endpoint below. Do not expose a localhost development
authentication bypass through a tunnel. Restore your normal SAS/grace settings
for production; the short values above are only for a quick local check.

## Remaining Azure bootstrap (manual; do not run as part of local tests)

1. In the workforce Entra tenant associated with the Azure subscription, create a
   single-tenant app registration named `EKH Storage Events`. This is separate
   from the External ID app used by React. Record its tenant ID, application
   (client) ID, and app object ID. Set `api.requestedAccessTokenVersion` to `2`
   in its manifest so the token audience is its client GUID. No client secret is
   needed by the webhook. Expose the default `api://<client-id>` Application ID URI.
2. Add an enabled application app role with both display name and value
   `AzureEventGridSecureWebhookSubscriber`. Follow Microsoft's linked bootstrap
   script to assign that role to the **Microsoft.EventGrid service principal**
   and to authorize the subscription writer. Find Microsoft.EventGrid under
   Enterprise Applications and verify its application ID for the cloud; in Azure
   public cloud it is normally `4962773b-9cdb-44cf-a8bf-237846a00ab7`. Put that
   exact ID in `storageEventsSenderApplicationId`. Do not put the subscription
   writer's ID there: the delivery policy admits only Event Grid. The person or
   deployment identity creating the subscription must also satisfy Event Grid's
   webhook ownership/app-role authorization check and hold Azure permission to
   create the subscription and role assignments. Use the official user/app
   variant appropriate to your deployment identity; bootstrap is not handled by Bicep.
3. Review/generate the Knowledge migration SQL and have your authorized deployment
   process apply it to Azure SQL before enabling the new worker. This implementation
   does not apply remote migrations. The App Service identity needs DML rights
   on Documents and UploadInbox; migration execution requires separate schema rights.
   This change also repairs discovery attributes on the earlier
   `20260927170000_AddDocumentUploadReservation` migration. Check its history entry
   and the UploadExpiresAt column on existing databases; if the column was added
   manually, reconcile migration history before applying the newly discoverable
   migration. Fresh databases apply the full chain automatically.
4. Set `enableStorageEventWebhook=true`, `storageEventsTenantId`,
   `storageEventsAudience=<webhook-client-GUID>`, and the verified sender ID in
   your environment's Bicep parameters. Leave `deployStorageEventSubscription=false`.
   Deploy the API code and App Service settings through the existing deployment
   process. Confirm it is running, Always On is true (already supported on B1/S1/P0v3),
   and unauthenticated POSTs to `/webhooks/storage-events` return 401.
5. Preserve the API managed identity's Storage Blob Data Contributor assignment
   at storage-account scope (includes user-delegation key generation). Keep
   `allowSharedKeyAccess=false`, `allowBlobPublicAccess=false`, private containers,
   HTTPS, and CORS restricted to the deployed frontend origins. Public **network**
   reachability remains enabled because browsers must reach the blob endpoint.
6. Only after the authenticated webhook is live, set
   `deployStorageEventSubscription=true` and deploy the Bicep template through
   your normal approved process. It creates a Storage system topic, a private
   `document-upload-events-deadletter` container, a narrowly scoped Blob Data Contributor
   assignment for the topic identity, and the webhook event subscription.
   Delivery uses Entra app authentication; dead-lettering uses the system topic
   managed identity. The subscription explicitly uses EventGridSchema, BlobCreated,
   the documents/organizations prefix, `.pdf` suffix and PutBlob filter, up to
   50 events per batch, 30 delivery attempts, and a 24-hour delivery TTL.
   Role propagation can require waiting before subscription creation succeeds.
7. Upload a small non-sensitive PDF from the deployed browser; verify the PUT
   returns 201, a second PUT cannot overwrite, the notification is committed,
   and the status becomes Uploaded. Test an invalid header, closing the tab after
   PUT, and worker downtime/lease recovery. These live checks are still required;
   local tests do not establish Azure delivery, Entra role propagation, or CORS.

The subscription's synchronous validation POST must pass the same bearer policy.
The endpoint returns `validationResponse` only after authentication and expected
source validation. Do not temporarily make it anonymous to bootstrap deployment.

## Operations and recovery

There are two different dead-letter locations: Event Grid's Storage container for
delivery failures, and `knowledge.UploadInbox` rows in DeadLetter for accepted
work that exhausted processing retries. Monitor both and worker logs. Transient
dependency failures leave the document pending, not falsely Rejected; operators
must recover exhausted reconciliation work. Retrying a failed cleanup must not
change its terminal Document state.

After resolving the dependency failure, an authorized operator can redrive a
specific inbox row (this is a manual production database operation, never automatic):

```sql
DECLARE @id uniqueidentifier = '<reviewed-dead-letter-work-id>';
UPDATE knowledge.UploadInbox
SET Status = 'Pending', Attempts = 0, NextAttemptAt = SYSUTCDATETIME(),
    LeaseOwner = NULL, LeaseExpiresAt = NULL, CompletedAt = NULL, LastError = NULL
WHERE Id = @id AND Status = 'DeadLetter';
```

Replayed Event Grid dead letters must pass through the same authenticated,
source-validated ingestion process. Do not grant browser accounts the machine
role just to replay an event. Existing reservations will also be reconciled, so
restoring processing and redriving its durable inbox is normally sufficient.
Keep Always On and monitor database/Storage availability: an API-process worker
cannot run while the app is stopped, but its durable work survives restarts.

## Future malware checks and document processing

Defender is not enabled, called, or presented as passed. Future scan results can
arrive before or after BlobCreated. Persist a valid result independently of the
current upload status and match it to the exact reservation and scanned ETag or
version; never discard a valid early result. Only an explicit accepted **clean**
result for the validated bytes may satisfy a required malware gate. Error,
NotScanned, missing results, and timeouts remain blocked when scanning is required.
Results for other bytes cannot clear the gate. A later scan-result event can use
the same authenticated inbox and lease worker with a dedicated handler. Keep
extraction and indexing as separate retryable steps. When implemented, update
DocumentProcessingGate and atomically record any scheduled work with the gated
document transition, preserving Organization ownership and immutable source version.

References: [Put Blob SAS permissions](https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob),
[Entra-secured Event Grid delivery](https://learn.microsoft.com/en-us/azure/event-grid/secure-webhook-delivery),
[user subscription-writer bootstrap](https://learn.microsoft.com/en-us/azure/event-grid/scripts/powershell-webhook-secure-delivery-microsoft-entra-user),
[application subscription-writer bootstrap](https://learn.microsoft.com/en-us/azure/event-grid/scripts/powershell-webhook-secure-delivery-microsoft-entra-app),
[Storage event schema](https://learn.microsoft.com/en-us/azure/event-grid/event-schema-blob-storage).
