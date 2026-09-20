using './main.bicep'

// Copy this file per environment or supply these non-secret values on the command line.
param namePrefix = 'ekh'
param environment = 'dev'
param location = 'westeurope'
param entraTenantId = '<entra-tenant-id>'
param entraClientId = '<api-application-client-id>'
param sqlEntraAdministratorName = '<entra-user-or-group-name>'
param sqlEntraAdministratorObjectId = '<entra-user-or-group-object-id>'
param apiImageTag = 'latest'
param appServicePlanSku = 'B1'
param staticWebAppSku = 'Free'
