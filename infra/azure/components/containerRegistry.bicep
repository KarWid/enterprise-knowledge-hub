targetScope = 'resourceGroup'

param name string
param location string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    // The API uses its managed identity, so an ACR administrator password is unnecessary.
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
  }
}

output name string = containerRegistry.name
output loginServer string = containerRegistry.properties.loginServer
