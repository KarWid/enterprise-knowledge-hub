targetScope = 'resourceGroup'

param name string
param location string
param allowedOrigins array

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

// The browser needs CORS only to PUT a blob at the single URI delegated by
// the API. This rule grants no storage permission by itself; the short-lived,
// blob-scoped user delegation SAS remains the authorization boundary.
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: allowedOrigins
          allowedMethods: [
            'PUT'
            'OPTIONS'
          ]
          allowedHeaders: [
            'if-none-match'
            'x-ms-blob-content-type'
            'x-ms-blob-type'
            'x-ms-version'
          ]
          exposedHeaders: [
            'ETag'
            'x-ms-request-id'
          ]
          maxAgeInSeconds: 600
        }
      ]
    }
  }
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'documents'
  properties: {
    publicAccess: 'None'
  }
}

output name string = storageAccount.name
output id string = storageAccount.id
output blobServiceUrl string = storageAccount.properties.primaryEndpoints.blob
