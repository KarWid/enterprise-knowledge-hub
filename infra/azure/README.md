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

Run the following in the `EnterpriseKnowledgeHub` database. Replace the values with the `apiAppName` and `apiManagedIdentityPrincipalId` outputs. This creates the runtime database user without granting schema-change permissions.

```sql
DECLARE @apiUserName sysname = N'<api-app-name>';
DECLARE @apiPrincipalId uniqueidentifier = '<api-managed-identity-principal-id>';
DECLARE @apiSid varbinary(16) = CONVERT(varbinary(16), @apiPrincipalId);

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @apiUserName)
BEGIN
    DECLARE @statement nvarchar(max) =
        N'CREATE USER ' + QUOTENAME(@apiUserName) +
        N' WITH SID = ' + sys.fn_varbintohexstr(@apiSid) + N', TYPE = E;';
    EXEC sys.sp_executesql @statement;
END;

ALTER ROLE db_datareader ADD MEMBER [<api-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<api-app-name>];
```

The `SID`/`TYPE = E` form avoids requiring SQL Server to query Microsoft Graph when resolving the managed identity. The API can read and write data but cannot alter the database schema.

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
