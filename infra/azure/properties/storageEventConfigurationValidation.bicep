targetScope = 'resourceGroup'

// This module deliberately creates no resources. Its constrained parameters
// make invalid cross-parameter combinations fail before App Service settings or
// the Event Grid subscription are deployed.

@description('Must be true whenever Storage Event webhook infrastructure is requested.')
@allowed([true])
param webhookEnabled bool

@description('Workforce Microsoft Entra tenant that issues Event Grid webhook tokens.')
@minLength(1)
param tenantId string

@description('Application (client) ID of the webhook application.')
@minLength(1)
param audience string

@description('Application ID of the Microsoft.EventGrid service principal.')
@minLength(1)
param senderApplicationId string

output configurationValidated bool = webhookEnabled && !empty(tenantId) && !empty(audience) && !empty(senderApplicationId)
