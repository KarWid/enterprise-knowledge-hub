targetScope = 'resourceGroup'

param name string
param location string

@allowed([
  'Free'
  'Standard'
])
param skuName string

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    // The React SPA routing rules live in src/Web/public/staticwebapp.config.json.
    allowConfigFileUpdates: true
  }
}

output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
