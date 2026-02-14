# Sample Data Seeding for GraphRAG Demo

This directory contains sample data and scripts for seeding the Cosmos DB and Azure AI Search with demo data.

## Sample Data Structure

### Entities
- **service-a**: A critical service in the system
- **process-x**: A business process
- **team-y**: The team responsible for escalations

### Relations
- service-a depends_on process-x
- process-x owned_by team-y
- service-a escalates_to team-y

### Chunks
Sample documentation chunks about services, processes, and teams with relevant entity linkages.

## Seeding Cosmos DB

### Prerequisites
- Azure CLI installed and logged in
- Cosmos DB account created
- Database and containers created:
  - Database: `graphrag`
  - Containers: `entities`, `relations`, `chunks`

### Using Azure CLI

```bash
# Set variables
COSMOS_ACCOUNT="your-cosmos-account"
RESOURCE_GROUP="your-resource-group"
DATABASE_ID="graphrag"

# Create database
az cosmosdb sql database create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --name $DATABASE_ID

# Create entities container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --database-name $DATABASE_ID \
  --name entities \
  --partition-key-path "/id"

# Create relations container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --database-name $DATABASE_ID \
  --name relations \
  --partition-key-path "/id"

# Create chunks container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --database-name $DATABASE_ID \
  --name chunks \
  --partition-key-path "/id"
```

### Sample Data Files

The `sample-data/` directory contains JSON files with sample entities, relations, and chunks.

To load the data:

```bash
# Install data migration tool (if not already installed)
dotnet tool install -g Microsoft.Azure.Cosmos.DataTransfer

# Get Cosmos connection string
COSMOS_CONN=$(az cosmosdb keys list --name $COSMOS_ACCOUNT --resource-group $RESOURCE_GROUP --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)

# Import entities
dmt -s json -si sample-data/entities.json \
  -t cosmos-nosql \
  -tc "$COSMOS_CONN" \
  --target-database graphrag \
  --target-container entities

# Import relations
dmt -s json -si sample-data/relations.json \
  -t cosmos-nosql \
  -tc "$COSMOS_CONN" \
  --target-database graphrag \
  --target-container relations

# Import chunks
dmt -s json -si sample-data/chunks.json \
  -t cosmos-nosql \
  -tc "$COSMOS_CONN" \
  --target-database graphrag \
  --target-container chunks
```

## Seeding Azure AI Search

### Prerequisites
- Azure CLI installed and logged in
- Azure AI Search service created
- Index created with the schema defined in `search-index-schema.json`

### Create Search Index

```bash
# Set variables
SEARCH_SERVICE="your-search-service"
SEARCH_KEY=$(az search admin-key show --service-name $SEARCH_SERVICE --resource-group $RESOURCE_GROUP --query "primaryKey" -o tsv)

# Create index
curl -X PUT \
  "https://$SEARCH_SERVICE.search.windows.net/indexes/chunks?api-version=2024-07-01" \
  -H "Content-Type: application/json" \
  -H "api-key: $SEARCH_KEY" \
  -d @search-index-schema.json

# Upload documents
curl -X POST \
  "https://$SEARCH_SERVICE.search.windows.net/indexes/chunks/docs/index?api-version=2024-07-01" \
  -H "Content-Type: application/json" \
  -H "api-key: $SEARCH_KEY" \
  -d @sample-data/search-documents.json
```

## Alternative: PowerShell Script

A PowerShell script is provided for Windows users: `seed-data.ps1`

```powershell
.\seed-data.ps1 -CosmosAccount "your-cosmos-account" -SearchService "your-search-service" -ResourceGroup "your-resource-group"
```

## Generating Embeddings

For vector search to work, you need to generate embeddings for the content. This can be done using Azure OpenAI:

```bash
# Install Azure OpenAI SDK (Python example)
pip install openai

# Run embedding generation script
python generate-embeddings.py \
  --cosmos-connection "$COSMOS_CONN" \
  --openai-endpoint "https://your-openai.openai.azure.com/" \
  --deployment "text-embedding-ada-002"
```

See `generate-embeddings.py` for the implementation.
