targetScope = 'resourceGroup'

// main.bicep is the composition root for Azure. It contains no individual
// resource definitions: it chooses names, calls each component, and connects
// their outputs to the inputs of the next component.

@description('Lowercase alphanumeric prefix used in globally unique resource names.')
@minLength(3)
@maxLength(8)
param namePrefix string

@description('Environment name, for example dev, test, or prod.')
@minLength(2)
@maxLength(8)
param environment string

param location string = resourceGroup().location

@description('Microsoft Entra External ID tenant ID that issues browser access tokens for the API.')
param apiEntraTenantId string

@description('External ID authority URL, for example https://contoso.ciamlogin.com/.')
param apiEntraAuthority string

@description('Application (client) ID of the API registration in Microsoft Entra External ID.')
param apiEntraClientId string

@description('Workforce Microsoft Entra tenant ID associated with the Azure subscription and Azure SQL server.')
param sqlEntraTenantId string

@description('Display name or user principal name of the Microsoft Entra user or group that administers Azure SQL.')
param sqlEntraAdministratorName string

@description('Object ID of the Microsoft Entra user or group that administers Azure SQL.')
param sqlEntraAdministratorObjectId string

@description('Image tag to configure on App Service. GitHub Actions replaces this with the commit SHA.')
param apiImageTag string = 'latest'

@allowed([
  'B1'
  'S1'
  'P0v3'
])
param appServicePlanSku string = 'B1'

@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Free'

module names 'properties/resourceNames.bicep' = {
  name: 'resource-names'
  params: {
    namePrefix: namePrefix
    environment: environment
  }
}

module roles 'properties/roleDefinitions.bicep' = {
  name: 'role-definitions'
}

module containerRegistry 'components/containerRegistry.bicep' = {
  name: 'container-registry'
  params: {
    name: names.outputs.containerRegistryName
    location: location
  }
}

module staticWebApp 'components/staticWebApp.bicep' = {
  name: 'static-web-app'
  params: {
    name: names.outputs.staticWebAppName
    location: location
    skuName: staticWebAppSku
  }
}

module sqlDatabase 'components/sqlDatabase.bicep' = {
  name: 'sql-database'
  params: {
    serverName: names.outputs.sqlServerName
    databaseName: 'EnterpriseKnowledgeHub'
    location: location
    sqlEntraTenantId: sqlEntraTenantId
    entraAdministratorName: sqlEntraAdministratorName
    entraAdministratorObjectId: sqlEntraAdministratorObjectId
  }
}

module appService 'components/appService.bicep' = {
  name: 'app-service'
  params: {
    appServicePlanName: names.outputs.appServicePlanName
    apiAppName: names.outputs.apiAppName
    location: location
    appServicePlanSku: appServicePlanSku
    containerRegistryName: containerRegistry.outputs.name
    sqlServerFullyQualifiedDomainName: sqlDatabase.outputs.fullyQualifiedDomainName
    sqlDatabaseName: sqlDatabase.outputs.databaseName
    staticWebAppHostname: staticWebApp.outputs.defaultHostname
    apiEntraTenantId: apiEntraTenantId
    apiEntraAuthority: apiEntraAuthority
    apiEntraClientId: apiEntraClientId
    apiImageTag: apiImageTag
    acrPullRoleDefinitionId: roles.outputs.acrPullRoleDefinitionId
  }
}

output apiAppName string = appService.outputs.name
output apiUrl string = 'https://${appService.outputs.defaultHostname}'
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'
output containerRegistryName string = containerRegistry.outputs.name
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output sqlServerFullyQualifiedDomainName string = sqlDatabase.outputs.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.outputs.databaseName
output apiManagedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
