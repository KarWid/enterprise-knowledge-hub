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

Use the same GitHub variables throughout these phases; no additional flag is needed.

| Phase | `ENABLE_STORAGE_EVENT_WEBHOOK` | `DEPLOY_STORAGE_EVENT_SUBSCRIPTION` | Result |
| --- | --- | --- | --- |
| 1: initial API deployment, if needed | `false` | `false` | API is deployed; webhook authentication fails closed. |
| 2: prepare authenticated webhook and Event Grid dependencies | `true` | `false` | Webhook settings, system topic, managed identity, separate dead-letter storage account/container and RBAC on that account are deployed. |
| 3: activate event delivery | `true` | `true` | Event subscription is created after phase 2 readiness checks. |

Complete the Entra bootstrap and database migration below before phase 2. A 401
from an unauthenticated POST in phase 1 proves neither that machine authentication
is enabled nor that Event Grid can deliver. Before phase 3, verify phase 2 settings,
API health, app-role assignments and storage RBAC. Successful subscription
validation in phase 3 checks the authenticated handshake.

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

   From the repository root, preview the role assignments with the parameterized
   bootstrap script:

   ```powershell
   .\infra\azure\bootstrap\Grant-StorageEventWebhookRoles.ps1 `
       -TenantId '<workforce-tenant-id>' `
       -WebhookApplicationId '<webhook-client-id>' `
       -EventGridApplicationId '<verified-Microsoft.EventGrid-app-id>' `
       -GitHubDeploymentApplicationId '<AZURE_CLIENT_ID>' `
       -WhatIf
   ```

   After checking the resolved application names, run the same command without
   `-WhatIf`. Add `-Confirm` if you want PowerShell to ask before each assignment.
3. Review/generate the Knowledge migration SQL and have your authorized deployment
   process apply it to Azure SQL before enabling the new worker. This implementation
   does not apply remote migrations. The App Service identity needs DML rights
   on Documents and UploadInbox; migration execution requires separate schema rights.
   This change also repairs discovery attributes on the earlier
   `20260927170000_AddDocumentUploadReservation` migration. Check its history entry
   and the UploadExpiresAt column on existing databases; if the column was added
   manually, reconcile migration history before applying the newly discoverable
   migration. Fresh databases apply the full chain automatically.
4. Set the GitHub repository variables `ENABLE_STORAGE_EVENT_WEBHOOK=true`,
   `STORAGE_EVENTS_AUDIENCE=<webhook-client-GUID>`, and
   `STORAGE_EVENTS_SENDER_APPLICATION_ID=<verified-Microsoft.EventGrid-app-ID>`.
   The workflow reuses `AZURE_TENANT_ID` as `storageEventsTenantId`. Leave
   `DEPLOY_STORAGE_EVENT_SUBSCRIPTION=false`.
   Run `Deploy Azure infrastructure` on the branch containing these Bicep changes.
   This preparation run also creates/reuses the system topic, its system-assigned
   identity, a dedicated Standard GPv2 / Hot / LRS storage account containing the
   private `document-upload-events-deadletter` container, and the topic identity's
   Storage Blob Data Contributor assignment on that new account. The topic still
   observes the original documents account. The API's BlobStorage settings and
   webhook ExpectedSource remain pointed at the original account. This run does
   not create the subscription yet. Deploy the API code through
   the existing API workflow if it is not already deployed. Confirm it is running,
   Always On is true (already supported on B1/S1/P0v3), `StorageEvents__Enabled`
   is true, the tenant/audience/sender settings match the Entra setup, and
   unauthenticated POSTs to `/webhooks/storage-events` return 401.
5. Preserve the API managed identity's Storage Blob Data Contributor assignment
   at storage-account scope (includes user-delegation key generation). Keep
   `allowSharedKeyAccess=false`, `allowBlobPublicAccess=false`, private containers,
   HTTPS, and CORS restricted to the deployed frontend origins. Public **network**
   reachability remains enabled because browsers must reach the blob endpoint.
6. Wait for the topic identity's dedicated dead-letter account RBAC assignment and the Entra
   app roles to propagate. Bicep `dependsOn` orders resource creation; it does
   not wait for authorization propagation. Check the topic identity and its
   assignment as shown below. Allow several minutes; an assignment visible in
   Azure Resource Manager does not guarantee that Event Grid already sees it.
   Only after the authenticated webhook and permissions are ready, set the GitHub repository
   variable `DEPLOY_STORAGE_EVENT_SUBSCRIPTION=true` and manually run the
   `Deploy Azure infrastructure` workflow again on the same branch/version.
   It reuses the prepared resources and creates the webhook event subscription.
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

### Existing deployments: move dead letters to the dedicated account

Keep the completed Entra app registration/role bootstrap and database migrations.
Only the last part of the deployment procedure changes:

1. Before merging these infrastructure changes into `master` (which triggers an
   automatic deployment), set `ENABLE_STORAGE_EVENT_WEBHOOK=true` and
   `DEPLOY_STORAGE_EVENT_SUBSCRIPTION=false` in GitHub. Deploy the updated Bicep
   using the merge-triggered run or a manual run on the updated branch.
2. Verify that the system topic identity has the role on the new dead-letter
   account, then allow time for RBAC propagation. Use Azure Cloud Shell PowerShell in the deployment
   subscription; the following commands are read-only:

   ```powershell
   $resourceGroupName = '<resource-group-name>'
   # Use main.bicep's storageAccountName and eventGridDeadLetterStorageAccountName outputs.
   $documentsStorageAccountName = '<original-documents-storage-account-name>'
   $deadLetterStorageAccountName = '<new-dead-letter-storage-account-name>'
   $systemTopicName = "$documentsStorageAccountName-uploads"

   $topicPrincipalId = az eventgrid system-topic show `
       --resource-group $resourceGroupName `
       --name $systemTopicName `
       --query identity.principalId --output tsv
   if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($topicPrincipalId))
   {
       throw 'Could not resolve the system topic managed identity.'
   }

   $deadLetterStorageAccountId = az storage account show `
       --resource-group $resourceGroupName `
       --name $deadLetterStorageAccountName `
       --query id --output tsv
   if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($deadLetterStorageAccountId))
   {
       throw 'Could not resolve the storage account.'
   }

   az role assignment list `
       --assignee-object-id $topicPrincipalId `
       --scope $deadLetterStorageAccountId `
       --include-inherited `
       --query "[?roleDefinitionName=='Storage Blob Data Contributor'].{Role:roleDefinitionName,Scope:scope,PrincipalId:principalId}" `
       --output table
   ```

   Check the principal against the topic identity and the scope against the new
   dead-letter account ID. The assignment must not target the documents account.
   The new direct assignment's scope ends with the dead-letter account name, not
   `/blobServices/default/containers/document-upload-events-deadletter`.
3. Set `DEPLOY_STORAGE_EVENT_SUBSCRIPTION=true` and run the infrastructure workflow
   again. Keep `ENABLE_STORAGE_EVENT_WEBHOOK=true`. Verify successful subscription
   creation/handshake, then test a fresh PDF upload and confirm an Event Grid
   notification (not just reconciliation) was persisted and processed in UploadInbox.
   In the subscription's dead-letter configuration, confirm the destination is
   the new account and the identity is SystemAssigned. The topic's source must
   still be the original documents account. An empty dead-letter container after
   successful uploads is expected; it does not itself prove dead-letter writes.
4. Complete the permission migration after the subscription switch succeeds.
   In the original documents account's Access control (IAM), identify any Storage
   Blob Data Contributor assignment for the system topic's exact principal ID.
   Also inspect the old `document-upload-events-deadletter` container's IAM for
   the earlier container-scoped assignment. An authorized operator must remove
   those old topic assignments at their exact scopes. Do not remove the API
   identity's assignment or the topic's role on the new dead-letter account.
   Confirm the topic has no remaining blob-data grants covering document storage,
   including inherited grants. Until this is done, old access remains effective.
5. Keep the old container and any dead-letter blobs for review/recovery. The
   template neither moves nor deletes them. Process or archive these notifications
   under the existing recovery procedure before separately approving their removal.

The account-scoped grant follows Microsoft's
[Event Grid managed-identity dead-letter setup](https://learn.microsoft.com/en-us/azure/event-grid/managed-service-identity).
It grants read/write/delete access across the dedicated account, so keep application
documents and other feature data in the original account. Dead letters contain
event notifications rather than PDF copies, but their identifiers still require
private access. Both accounts disable anonymous access and shared keys and require
HTTPS. Event Grid uses the dedicated account's public network endpoint with its
managed identity; no browser CORS or API role is configured there. No Defender
or automatic retention deletion is enabled by these templates.

If you already manually added an assignment on the new account, check its resource name
before deployment: an existing assignment with another name can conflict with the
Bicep-managed one and needs explicit adoption rather than repeated creation.

In incremental mode, switching a conditional resource off does not delete an
existing subscription. These flags control initial deployment ordering, not a
runtime pause switch. Leave both true after activation for normal updates.
Do not disable anonymous-access protection or enable storage keys to resolve
authorization failures. If the account-scoped assignment exists and retries still
fail, compare the failing identity/resource IDs and inspect deployment operations
and storage network restrictions before changing permissions again.

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
