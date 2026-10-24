targetScope = 'resourceGroup'

param storageAccountName string
param documentsContainerName string
param location string
@secure()
param webhookUrl string
param webhookTenantId string
param webhookAudience string

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource deadletters 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'document-upload-events-deadletter'
  properties: { publicAccess: 'None' }
}

resource topic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: '${storageAccountName}-uploads'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    source: storage.id
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

resource deadletterAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: deadletters
  name: guid(deadletters.id, topic.id, 'deadletter-writer')
  properties: {
    principalId: topic.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

// Deploy only AFTER the API/webhook and Entra role assignment are live.
resource uploadEvents 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = {
  parent: topic
  name: 'document-uploads'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: webhookUrl
        azureActiveDirectoryTenantId: webhookTenantId
        azureActiveDirectoryApplicationIdOrUri: webhookAudience
        maxEventsPerBatch: 50
        preferredBatchSizeInKilobytes: 256
      }
    }
    filter: {
      includedEventTypes: ['Microsoft.Storage.BlobCreated']
      subjectBeginsWith: '/blobServices/default/containers/${documentsContainerName}/blobs/organizations/'
      subjectEndsWith: '.pdf'
      isSubjectCaseSensitive: true
      advancedFilters: [
        { operatorType: 'StringIn', key: 'data.api', values: ['PutBlob'] }
      ]
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
    deadLetterWithResourceIdentity: {
      identity: { type: 'SystemAssigned' }
      deadLetterDestination: {
        endpointType: 'StorageBlob'
        properties: {
          resourceId: storage.id
          blobContainerName: deadletters.name
        }
      }
    }
  }
  dependsOn: [deadletterAccess]
}
