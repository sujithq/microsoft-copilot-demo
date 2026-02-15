# Terraform Infrastructure for GraphRAG Demo

This directory contains Terraform configuration for deploying the GraphRAG demo infrastructure to Azure.

## Microsoft Foundry Note

**Important**: This project now uses **Microsoft Foundry** (formerly Azure AI Foundry). Microsoft Foundry uses a hub-project architecture:

- **Hub**: Central management resource for AI projects
- **Projects**: Isolated workspaces for AI development

Since Terraform doesn't fully support Microsoft Foundry hub-project resources yet, we use a hybrid approach:

1. **Terraform**: Provisions base resources (AI Services, Cosmos DB, Search, App Service)
2. **az cli** (`scripts/provision-foundry.sh`): Creates full Foundry hub with project management

## Prerequisites

- [Terraform](https://www.terraform.io/downloads.html) >= 1.0
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- Azure subscription with appropriate permissions

## Resources Created

The Terraform configuration creates the following Azure resources:

1. **Resource Group**: Container for all resources
2. **Cosmos DB Account**: NoSQL database for entities, relations, and chunks
   - Database: `graphrag`
   - Containers: `entities`, `relations`, `chunks`
3. **Azure AI Search**: Service for hybrid search (BM25 + vector)
4. **Microsoft Foundry Hub (Base)**: AI Services account that serves as foundation for Foundry hub with OpenAI capabilities
5. **App Service Plan**: Linux-based hosting plan (B1 SKU)
6. **App Service**: Web app for the Orchestrator API with Managed Identity
7. **Role Assignments**: Managed Identity permissions for accessing Azure services

**Note**: For full Microsoft Foundry hub with project management, run `scripts/provision-foundry.sh` after Terraform deployment.

## Quick Start

### 1. Login to Azure

```bash
az login
az account set --subscription "<your-subscription-id>"
```

### 2. Initialize Terraform

```bash
cd infra/terraform
terraform init
```

### 3. Configure Variables

Copy the example variables file and customize it:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars`:

```hcl
environment = "dev"
location    = "eastus"
name_prefix = "graphrag"
openai_deployment_name = "gpt-5.2"
```

### 4. Plan Deployment

Review the resources that will be created:

```bash
terraform plan
```

### 5. Deploy Infrastructure

```bash
terraform apply
```

Type `yes` when prompted to confirm the deployment.

### 6. View Outputs

After deployment completes, view the resource endpoints:

```bash
terraform output
```

To view sensitive outputs (like keys):

```bash
terraform output cosmos_primary_key
terraform output search_primary_key
```

## Configuration Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `environment` | Environment name (dev, test, prod) | `dev` | No |
| `location` | Azure region | `eastus` | No |
| `name_prefix` | Prefix for resource names | `graphrag` | No |
| `openai_deployment_name` | OpenAI model deployment name | `gpt-5.2` | No |

## Outputs

| Output | Description |
|--------|-------------|
| `resource_group_name` | Name of the created resource group |
| `cosmos_endpoint` | Cosmos DB endpoint URL |
| `cosmos_primary_key` | Cosmos DB primary key (sensitive) |
| `search_endpoint` | Azure AI Search endpoint URL |
| `search_primary_key` | Azure AI Search admin key (sensitive) |
| `openai_endpoint` | Microsoft Foundry (AI Services) endpoint URL |
| `app_service_url` | Orchestrator API URL |
| `app_service_name` | App Service name |
| `app_service_principal_id` | Managed Identity Principal ID |

## Security

The Terraform configuration implements the following security best practices:

1. **Managed Identity**: App Service uses System-Assigned Managed Identity
2. **RBAC**: Least-privilege role assignments for accessing Azure services
3. **No Keys in Code**: Sensitive keys are only exposed through Terraform outputs
4. **Secure Communication**: All services use HTTPS

### Role Assignments

The App Service Managed Identity is granted the following roles:

- **Cosmos DB Data Contributor**: Read/write access to Cosmos DB
- **Cognitive Services OpenAI User**: Access to Microsoft Foundry models
- **Search Index Data Contributor**: Read/write access to search indexes

## Post-Deployment Steps

After deploying the infrastructure:

1. **Provision Microsoft Foundry Hub and Projects**:
   ```bash
   cd ../../scripts
   RESOURCE_GROUP=$(terraform output -raw resource_group_name)
   LOCATION="eastus"
   
   # This creates the full Foundry hub with project management
   ./provision-foundry.sh $RESOURCE_GROUP $LOCATION
   ```

2. **Verify Foundry Hub**:
   Visit [Microsoft Foundry Portal](https://ai.azure.com) to see your hub and create projects

3. **Create Azure AI Search Index**:
   ```bash
   cd ../../scripts
   SEARCH_ENDPOINT=$(terraform output -raw search_endpoint)
   SEARCH_KEY=$(terraform output -raw search_primary_key)
   
   curl -X PUT \
     "$SEARCH_ENDPOINT/indexes/chunks?api-version=2024-07-01" \
     -H "Content-Type: application/json" \
     -H "api-key: $SEARCH_KEY" \
     -d @search-index-schema.json
   ```

3. **Seed Sample Data**: See `scripts/README.md` for data seeding instructions

4. **Deploy Application**:
   ```bash
   cd ../../src/OrchestratorAPI
   dotnet publish -c Release -o ./publish
   
   APP_NAME=$(terraform output -raw app_service_name)
   RESOURCE_GROUP=$(terraform output -raw resource_group_name)
   
   az webapp deploy \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --src-path ./publish \
     --type zip
   ```

## State Management

### Local State (Default)

By default, Terraform stores state locally in `terraform.tfstate`. This is suitable for development but not recommended for production.

### Remote State (Recommended for Teams)

For team collaboration, use Azure Storage for remote state:

1. Create a storage account for state:
   ```bash
   az storage account create \
     --name tfstate$RANDOM \
     --resource-group terraform-state-rg \
     --location eastus \
     --sku Standard_LRS
   
   az storage container create \
     --name tfstate \
     --account-name <storage-account-name>
   ```

2. Configure backend in `main.tf`:
   ```hcl
   terraform {
     backend "azurerm" {
       resource_group_name  = "terraform-state-rg"
       storage_account_name = "<storage-account-name>"
       container_name       = "tfstate"
       key                  = "graphrag.tfstate"
     }
   }
   ```

3. Re-initialize Terraform:
   ```bash
   terraform init -migrate-state
   ```

## Cleanup

To destroy all created resources:

```bash
terraform destroy
```

Type `yes` when prompted to confirm destruction.

## Troubleshooting

### Authentication Issues

If you encounter authentication errors:

```bash
az login
az account set --subscription "<your-subscription-id>"
```

### Provider Registration

If you see provider registration errors:

```bash
az provider register --namespace Microsoft.DocumentDB
az provider register --namespace Microsoft.Search
az provider register --namespace Microsoft.CognitiveServices
az provider register --namespace Microsoft.Web
```

### Resource Name Conflicts

If resource names are already taken, modify the `name_prefix` variable in `terraform.tfvars`.

## Additional Resources

- [Terraform Azure Provider Documentation](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Azure Cosmos DB Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/)
- [Azure AI Search Documentation](https://docs.microsoft.com/en-us/azure/search/)
- [Microsoft Foundry Documentation](https://learn.microsoft.com/azure/ai-foundry/)
- [Microsoft Foundry Hub-Project Architecture](https://learn.microsoft.com/azure/ai-foundry/concepts/ai-resources)
- [Azure OpenAI Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
