// Main Bicep template for GraphRAG Demo Infrastructure

@description('The name of the environment (e.g., dev, test, prod)')
param environmentName string = 'dev'

@description('The Azure region for all resources')
param location string = resourceGroup().location

@description('The name prefix for all resources')
param namePrefix string = 'graphrag'

// Variables
var uniqueSuffix = substring(uniqueString(resourceGroup().id), 0, 6)
var cosmosAccountName = '${namePrefix}-cosmos-${uniqueSuffix}'
var searchServiceName = '${namePrefix}-search-${uniqueSuffix}'
var openAIAccountName = '${namePrefix}-openai-${uniqueSuffix}'
var appServicePlanName = '${namePrefix}-asp-${uniqueSuffix}'
var appServiceName = '${namePrefix}-api-${uniqueSuffix}'

// Cosmos DB Account
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
  }
}

// Cosmos DB Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosAccount
  name: 'graphrag'
  properties: {
    resource: {
      id: 'graphrag'
    }
    options: {
      throughput: 400
    }
  }
}

// Cosmos DB Containers
resource entitiesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'entities'
  properties: {
    resource: {
      id: 'entities'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

resource relationsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'relations'
  properties: {
    resource: {
      id: 'relations'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

resource chunksContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'chunks'
  properties: {
    resource: {
      id: 'chunks'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

// Azure AI Search
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
  }
}

// Azure OpenAI
resource openAIAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIAccountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIAccountName
    publicNetworkAccess: 'Enabled'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {}
}

// App Service for Orchestrator API
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'CosmosDb__Endpoint'
          value: cosmosAccount.properties.documentEndpoint
        }
        {
          name: 'AzureSearch__Endpoint'
          value: 'https://${searchService.name}.search.windows.net'
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAIAccount.properties.endpoint
        }
        {
          name: 'AzureOpenAI__DeploymentName'
          value: 'gpt-4'
        }
      ]
      netFrameworkVersion: 'v10.0'
    }
  }
}

// Role Assignments for Managed Identity

// Cosmos DB Data Contributor
resource cosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  parent: cosmosAccount
  name: guid(appService.id, cosmosAccount.id, 'contributor')
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: appService.identity.principalId
    scope: cosmosAccount.id
  }
}

// Outputs
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output openAIEndpoint string = openAIAccount.properties.endpoint
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePrincipalId string = appService.identity.principalId
