using './main.bicep'

// Copy this file per environment or supply these non-secret values on the command line.
param namePrefix = 'ekh'
param environment = 'dev'
param location = 'westeurope'
// External ID values used to validate browser access tokens in the API.
param apiEntraTenantId = '<external-id-tenant-id>'
param apiEntraAuthority = 'https://<external-id-subdomain>.ciamlogin.com/'
param apiEntraClientId = '<external-id-api-application-client-id>'

// This is the workforce Entra tenant associated with the Azure subscription.
// It owns the SQL administrator group and the App Service managed identity.
param sqlEntraTenantId = '<azure-workforce-tenant-id>'
param sqlEntraAdministratorName = '<entra-user-or-group-name>'
param sqlEntraAdministratorObjectId = '<entra-user-or-group-object-id>'
param apiImageTag = 'latest'
param appServicePlanSku = 'B1'
param staticWebAppSku = 'Free'
