targetScope = 'resourceGroup'

param storageAccountName string
param documentsContainerName string
param deadLetterStorageAccountName string
param deadLetterContainerName string
param location string
@description('Create the subscription only after a preparation deployment and RBAC propagation.')
param deploySubscription bool = false
param storageBlobDataContributorRoleDefinitionId string
@secure()
param webhookUrl string
param webhookTenantId string
param webhookAudience string

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource deadLetterStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: deadLetterStorageAccountName
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
  // Event Grid's documented dead-letter setup grants this role on the account.
  // Restrict the grant to the dedicated dead-letter account, not document storage.
  scope: deadLetterStorage
  name: guid(deadLetterStorage.id, topic.id, 'deadletter-writer')
  properties: {
    principalId: topic.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRoleDefinitionId
  }
}

// Deploy only AFTER the webhook, Entra app roles and topic RBAC are ready.
// dependsOn orders creation; it cannot wait for RBAC propagation.
resource uploadEvents 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = if (deploySubscription) {
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
          resourceId: deadLetterStorage.id
          blobContainerName: deadLetterContainerName
        }
      }
    }
  }
  dependsOn: [deadletterAccess]
}
