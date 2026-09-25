targetScope = 'resourceGroup'

param appServicePlanName string
param apiAppName string
param location string
param appServicePlanSku string
param containerRegistryName string
param sqlServerFullyQualifiedDomainName string
param sqlDatabaseName string
param staticWebAppHostname string
// Browser users authenticate through Microsoft Entra External ID. These
// values are independent of the workforce Entra tenant that owns Azure SQL.
param apiEntraTenantId string
param apiEntraAuthority string
param apiEntraClientId string
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
  // The image itself is deliberately not declared here. The API deployment
  // workflow configures an immutable commit-SHA image after it builds it.
  kind: 'app,linux'
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
    // External ID uses a ciamlogin.com authority, not login.microsoftonline.com.
    AzureAd__Authority: apiEntraAuthority
    AzureAd__ClientId: apiEntraClientId
    // The SPA requests api://<client-id>/access_as_user. The resulting access
    // token has api://<client-id> as its audience, which the API must validate.
    AzureAd__Audience: 'api://${apiEntraClientId}'
    AzureAd__TenantId: apiEntraTenantId
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
