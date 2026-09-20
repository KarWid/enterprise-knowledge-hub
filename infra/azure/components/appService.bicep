targetScope = 'resourceGroup'

param appServicePlanName string
param apiAppName string
param location string
param appServicePlanSku string
param containerRegistryName string
param sqlServerFullyQualifiedDomainName string
param sqlDatabaseName string
param staticWebAppHostname string
param entraTenantId string
param entraClientId string
param apiImageTag string
param acrPullRoleDefinitionId string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServicePlanSku
    tier: startsWith(appServicePlanSku, 'B') ? 'Basic' : startsWith(appServicePlanSku, 'S') ? 'Standard' : 'PremiumV3'
  }
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2024-04-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      linuxFxVersion: 'DOCKER|${containerRegistryName}.azurecr.io/enterpriseknowledgehub-api:${apiImageTag}'
      minTlsVersion: '1.2'
      acrUseManagedIdentityCreds: true
    }
  }
}

resource apiAppSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: apiApp
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    ASPNETCORE_URLS: 'http://+:8080'
    WEBSITES_PORT: '8080'
    AzureAd__Authority: '${az.environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
    AzureAd__ClientId: entraClientId
    AzureAd__TenantId: entraTenantId
    AzureAd__Scopes: 'access_as_user'
    // The browser application is the only production origin allowed by CORS.
    Cors__AllowedOrigins__0: 'https://${staticWebAppHostname}'
    // In Azure, Active Directory Default resolves to this App Service's
    // system-assigned managed identity. No SQL password is configured.
    ConnectionStrings__EnterpriseKnowledgeHubDbConnectionString: 'Server=tcp:${sqlServerFullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}

resource apiAcrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, apiApp.id, acrPullRoleDefinitionId)
  properties: {
    principalId: apiApp.identity.principalId
    roleDefinitionId: acrPullRoleDefinitionId
    principalType: 'ServicePrincipal'
  }
}

output name string = apiApp.name
output defaultHostname string = apiApp.properties.defaultHostName
output managedIdentityPrincipalId string = apiApp.identity.principalId
