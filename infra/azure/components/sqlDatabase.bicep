targetScope = 'resourceGroup'

param serverName string
param databaseName string
param location string
// Azure SQL identities belong to the workforce Entra tenant associated with
// the Azure subscription. This is deliberately not the External ID tenant
// that signs in the application's customers.
param sqlEntraTenantId string
param entraAdministratorName string
param entraAdministratorObjectId string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    // Entra-only authentication removes the SQL administrator password and
    // prevents SQL-authentication connections to this logical server.
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: entraAdministratorName
      sid: entraAdministratorObjectId
      tenantId: sqlEntraTenantId
    }
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

// This permits Azure-hosted services, including our App Service, to connect to
// the server. It does not open the server to arbitrary internet clients.
resource allowAzureServicesFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

output serverName string = sqlServer.name
output databaseName string = sqlDatabase.name
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
