targetScope = 'resourceGroup'

// Bicep modules cannot import ordinary variables from another file. This small
// module is our shared "properties" file: main.bicep reads these outputs once
// and passes the names to the components that need them.
@minLength(3)
@maxLength(8)
param namePrefix string

@minLength(2)
@maxLength(8)
param environment string

var suffix = toLower(take(uniqueString(subscription().subscriptionId, resourceGroup().id, namePrefix, environment), 6))
var resourceBaseName = toLower('${namePrefix}-${environment}-${suffix}')

output containerRegistryName string = toLower('acr${namePrefix}${environment}${suffix}')
output sqlServerName string = '${resourceBaseName}-sql'
output appServicePlanName string = '${resourceBaseName}-plan'
output apiAppName string = '${resourceBaseName}-api'
output staticWebAppName string = '${resourceBaseName}-web'
