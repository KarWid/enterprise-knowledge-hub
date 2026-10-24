targetScope = 'resourceGroup'

// Azure built-in role definition IDs. Keeping them in one place avoids magic
// strings in the App Service component.
output acrPullRoleDefinitionId string = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

output storageBlobDataContributorRoleDefinitionId string = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
)
