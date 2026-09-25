# First Azure deployment

This infrastructure deliberately contains only the services used by the current application:

- Azure SQL Database with Microsoft Entra-only authentication
- Azure Container Registry (ACR) for the API image
- Linux App Service for the API container
- Azure Static Web Apps for React

Key Vault is not deployed yet. The application no longer has a secret to store: App Service connects to SQL through its system-assigned managed identity. Add Key Vault only when the application has a real secret that cannot be eliminated.

## Folder layout

```text
infra/azure/
├── main.bicep                 # The composition root: deploy this file.
├── main.bicepparam            # Safe example values.
├── properties/
│   ├── resourceNames.bicep    # One deterministic name for every Azure resource.
│   └── roleDefinitions.bicep  # Azure built-in RBAC role IDs.
└── components/
    ├── containerRegistry.bicep
    ├── sqlDatabase.bicep
    ├── staticWebApp.bicep
    └── appService.bicep
```

`main.bicep` is the only entry point. It calls each component and passes outputs forward. For example, SQL returns its server name and database name; App Service uses them to create the passwordless connection string.

The infrastructure deployment creates the App Service and its managed identity, but does not choose an API image. `deploy-api.yml` is the sole owner of the container image and deploys an immutable commit-SHA tag. This prevents a later infrastructure deployment from overwriting a working API image with a floating `latest` tag.

## Two Microsoft Entra directories

This deployment deliberately separates two identity responsibilities:

| Directory | Purpose | Bicep values |
| --- | --- | --- |
| Microsoft Entra External ID | The React SPA and API registrations; it issues customer access tokens. | `apiEntraTenantId`, `apiEntraAuthority`, `apiEntraClientId` |
| Workforce Microsoft Entra tenant associated with the Azure subscription | GitHub deployment identity, App Service managed identity, and Azure SQL administrator group. | `sqlEntraTenantId` |

Do not put the External ID tenant ID in `sqlEntraTenantId`. Azure SQL must use the workforce tenant that owns the Azure subscription.

## Is it safe to deploy twice?

Yes. Azure Resource Manager uses **Incremental** deployment by default:

- The first run creates resources.
- A later run finds resources with the same stable names and updates only configuration that changed.
- Role-assignment names are deterministic, so rerunning does not create duplicate `AcrPull` assignments.
- Removing a resource from Bicep does not delete it from Azure in Incremental mode. Delete resources deliberately after checking their data and dependencies.

Keep `namePrefix` and `environment` stable for an environment. Changing either changes generated names and creates a separate set of resources. Do not use Complete mode for this project because it can delete resources absent from the template.

## SQL authentication model

The logical SQL server is configured with a Microsoft Entra administrator and `azureADOnlyAuthentication: true`. SQL usernames and passwords cannot connect to it. The API connection string uses `Authentication=Active Directory Default`; in App Service this resolves to the app's system-assigned managed identity.

The SQL firewall still permits Azure-hosted services so App Service can reach the public SQL endpoint. Managed identity is authentication, not network access. Private networking can replace this later.

## First deployment

Before deployment, choose an Entra user or, preferably, an Entra security group that will administer the SQL server. You need its display name and object ID. The people who run migrations must be members of that group.

```powershell
az deployment group create `
  --resource-group <resource-group> `
  --template-file infra/azure/main.bicep `
  --parameters infra/azure/main.bicepparam `
  --parameters `
    apiEntraTenantId='<external-id-tenant-id>' `
    apiEntraAuthority='https://<external-id-subdomain>.ciamlogin.com/' `
    apiEntraClientId='<external-id-api-client-id>' `
    sqlEntraTenantId='<azure-workforce-tenant-id>' `
    sqlEntraAdministratorName='<entra-user-or-group-name>' `
    sqlEntraAdministratorObjectId='<entra-user-or-group-object-id>'
```

Record the deployment outputs: `apiUrl`, `staticWebAppUrl`, `apiAppName`, `apiManagedIdentityPrincipalId`, `sqlServerFullyQualifiedDomainName`, and `sqlDatabaseName`.

## One-time database access setup

Bicep deploys Azure resources but does not execute T-SQL inside the database. After App Service exists, connect to the new database as the configured Entra SQL administrator using Azure Data Studio, SSMS, or the Azure portal Query Editor.

If you connect from your computer, first add your public IP under the SQL server's **Networking** page in the Azure portal. The `AllowAzureServices` rule does not include your laptop.

Run the following in the `EnterpriseKnowledgeHub` database. This creates the runtime database user without granting schema-change permissions.

First obtain the managed identity's **Application (client) ID**. The Bicep output named `apiManagedIdentityPrincipalId` is the identity's **Object (principal) ID**, which is useful as the lookup input but must not be used as the SQL user SID for an application identity.

```powershell
az ad sp show `
  --id <api-managed-identity-principal-id> `
  --query appId `
  --output tsv
```

Use the returned client ID in `@apiClientId` below. The SQL Entra administrator group continues to use its **Object ID**; this distinction is important because it is a group rather than an application identity.

```sql
USE [EnterpriseKnowledgeHub];
GO

-- Exact Azure App Service name.
DECLARE @apiUserName sysname = N'<api-app-name>';

-- App Service managed identity Application (client) ID. Do not use its Object ID.
DECLARE @apiClientId uniqueidentifier = '<api-managed-identity-client-id>';
DECLARE @apiSid nvarchar(max) =
    CONVERT(nvarchar(max), CONVERT(varbinary(16), @apiClientId), 1);

DECLARE @quotedApiUserName nvarchar(258) =
    N'[' + REPLACE(@apiUserName, N']', N']]') + N']';

-- Initial setup: create the contained Entra application user once.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @apiUserName)
BEGIN
    EXEC(
        N'CREATE USER ' + @quotedApiUserName +
        N' WITH SID = ' + @apiSid + N', TYPE = E;'
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_datareader'
        AND memberPrincipal.name = @apiUserName
)
BEGIN
    EXEC(N'ALTER ROLE db_datareader ADD MEMBER ' + @quotedApiUserName + N';');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_datawriter'
        AND memberPrincipal.name = @apiUserName
)
BEGIN
    EXEC(N'ALTER ROLE db_datawriter ADD MEMBER ' + @quotedApiUserName + N';');
END;
```

The `SID`/`TYPE = E` form avoids requiring SQL Server to query Microsoft Graph when resolving the managed identity. The API can read and write data but cannot alter the database schema.

Do not drop and recreate this user during ordinary API deployments. A system-assigned managed identity changes only when the App Service resource is deleted and recreated. If that happens, or the API logs `Login failed for user '<token-identified principal>'`, confirm that the stored SID matches the current managed identity client ID. If it does not, run `DROP USER [<api-app-name>]` in `EnterpriseKnowledgeHub`, then rerun the script above with the new client ID.

## Run EF Core migrations manually

Run migrations as your Entra SQL administrator, not as the API managed identity. That keeps DDL permissions out of the running web application.

```powershell
az login

$env:ConnectionStrings__EnterpriseKnowledgeHubDbConnectionString = `
  'Server=tcp:<sql-server-fqdn>,1433;Initial Catalog=EnterpriseKnowledgeHub;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

Push-Location src
dotnet ef database update --project Modules/Organizations/EnterpriseKnowledgeHub.Modules.Organizations --startup-project Api/EnterpriseKnowledgeHub.Api --context OrganizationsDbContext
dotnet ef database update --project Modules/Identity/EnterpriseKnowledgeHub.Modules.Identity --startup-project Api/EnterpriseKnowledgeHub.Api --context IdentityDbContext
Pop-Location

Remove-Item Env:ConnectionStrings__EnterpriseKnowledgeHubDbConnectionString
```

## GitHub Actions setup

All deployment workflows run only after a push to `master`.

### 1. Configure Azure login with OpenID Connect

Create a Microsoft Entra application/service principal for GitHub Actions and configure a federated credential for this repository's `master` branch. Store the following GitHub **repository variables**:

| Variable | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | GitHub deployment application's client ID |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_RESOURCE_GROUP` | Resource group containing these resources |
| `AZURE_NAME_PREFIX` | Same lowercase name prefix supplied to Bicep |
| `AZURE_ENVIRONMENT` | Same environment supplied to Bicep, for example `dev` |
| `AZURE_LOCATION` | Azure region, for example `westeurope` |
| `AZURE_APP_SERVICE_PLAN_SKU` | `B1`, `S1`, or `P0v3` |
| `AZURE_STATIC_WEB_APP_SKU` | `Free` or `Standard` |
| `AZURE_CONTAINER_REGISTRY_NAME` | Bicep `containerRegistryName` output |
| `AZURE_API_APP_NAME` | Bicep `apiAppName` output |
| `ENTRA_TENANT_ID` | Microsoft Entra External ID tenant ID used by the API |
| `ENTRA_AUTHORITY` | External ID authority, for example `https://<external-id-subdomain>.ciamlogin.com/` |
| `ENTRA_API_CLIENT_ID` | API app registration client ID from Microsoft Entra External ID |
| `SQL_ENTRA_ADMINISTRATOR_NAME` | SQL Entra administrator group display name from the workforce tenant |
| `SQL_ENTRA_ADMINISTRATOR_OBJECT_ID` | SQL Entra administrator group object ID from the workforce tenant |

`AZURE_TENANT_ID` is the workforce tenant ID. The workflow also passes it to SQL as `sqlEntraTenantId`; there is no separate GitHub variable to maintain.

The deployment identity needs `Contributor` on the resource group and `User Access Administrator` because Bicep creates an RBAC assignment. It also needs `AcrPush` on ACR to build and push API images.

### 2. Add the Static Web Apps token

Add one GitHub secret, `AZURE_STATIC_WEB_APPS_API_TOKEN`, after the infrastructure deployment creates the Static Web App. Retrieve it from the Azure portal. Never commit it.

### 3. Add frontend repository variables

These Vite values are embedded in the browser bundle and must not contain secrets.

| Variable | Value |
| --- | --- |
| `VITE_API_URL` | Bicep `apiUrl` output |
| `VITE_ENTRA_CLIENT_ID` | SPA app registration client ID |
| `VITE_ENTRA_AUTHORITY` | Entra / External ID authority URL |
| `VITE_ENTRA_ISSUER_HOST` | Issuer hostname used by MSAL |
| `VITE_ENTRA_REDIRECT_URI` | Bicep `staticWebAppUrl` output |
| `VITE_ENTRA_API_CLIENT_ID` | API scope, for example `api://<api-client-id>/access_as_user` |

Register `VITE_ENTRA_REDIRECT_URI` as a redirect URI in the SPA Microsoft Entra application before users sign in.
