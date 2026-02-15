#!/bin/bash

# GraphRAG Demo - Infrastructure Deployment Script (Terraform)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check if Terraform is installed
if ! command -v terraform &> /dev/null; then
    print_error "Terraform is not installed. Please install it first."
    echo "Visit: https://www.terraform.io/downloads.html"
    exit 1
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Parse arguments
ENVIRONMENT=${1:-dev}
LOCATION=${2:-northeurope}
NAME_PREFIX=${3:-graphrag}

print_info "Deploying GraphRAG infrastructure with Terraform..."
print_info "Environment: $ENVIRONMENT"
print_info "Location: $LOCATION"
print_info "Name Prefix: $NAME_PREFIX"
echo ""

# Change to Terraform directory
cd "$(dirname "$0")/terraform"

# Check if terraform.tfvars exists
if [ ! -f "terraform.tfvars" ]; then
    print_warning "terraform.tfvars not found. Creating from example..."
    cat > terraform.tfvars << EOF
environment = "$ENVIRONMENT"
location    = "$LOCATION"
name_prefix = "$NAME_PREFIX"
EOF
    print_info "Created terraform.tfvars. You can customize it if needed."
fi

# Initialize Terraform
print_info "Initializing Terraform..."
terraform init

# Validate configuration
print_info "Validating Terraform configuration..."
terraform validate

# Plan deployment
print_info "Creating deployment plan..."
terraform plan -out=tfplan

# Apply deployment
print_info "Applying Terraform configuration..."
terraform apply tfplan

# Extract outputs
print_info "Deployment completed successfully!"
echo ""

RESOURCE_GROUP=$(terraform output -raw resource_group_name)
COSMOS_ENDPOINT=$(terraform output -raw cosmos_endpoint)
SEARCH_ENDPOINT=$(terraform output -raw search_endpoint)
OPENAI_ENDPOINT=$(terraform output -raw openai_endpoint)
APP_SERVICE_URL=$(terraform output -raw app_service_url)
APP_SERVICE_NAME=$(terraform output -raw app_service_name)

print_info "Resource endpoints:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Cosmos DB: $COSMOS_ENDPOINT"
echo "  AI Search: $SEARCH_ENDPOINT"
echo "  Azure OpenAI: $OPENAI_ENDPOINT"
echo "  App Service: $APP_SERVICE_URL"
echo ""

# Save configuration
CONFIG_FILE="../deployment-config-${ENVIRONMENT}.env"
cat > $CONFIG_FILE << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
COSMOS_ENDPOINT=$COSMOS_ENDPOINT
SEARCH_ENDPOINT=$SEARCH_ENDPOINT
OPENAI_ENDPOINT=$OPENAI_ENDPOINT
APP_SERVICE_URL=$APP_SERVICE_URL
APP_SERVICE_NAME=$APP_SERVICE_NAME
EOF

print_info "Configuration saved to: $CONFIG_FILE"
echo ""

print_warning "Next steps:"
echo ""
echo "1. Provision Microsoft Foundry Hub and Projects:"
echo "   cd ../scripts"
echo "   ./provision-foundry.sh $RESOURCE_GROUP $LOCATION"
echo ""
echo "2. Deploy gpt-5.2 model via script (automated) or portal"
echo "   The provision-foundry.sh script will attempt automatic deployment"
echo ""
echo "3. Create Azure AI Search index:"
echo "   cd ../../scripts"
echo "   SEARCH_KEY=\$(terraform output -raw search_primary_key)"
echo "   curl -X PUT \"$SEARCH_ENDPOINT/indexes/chunks?api-version=2024-07-01\" \\"
echo "     -H \"Content-Type: application/json\" \\"
echo "     -H \"api-key: \$SEARCH_KEY\" \\"
echo "     -d @search-index-schema.json"
echo ""
echo "3. Seed sample data: see scripts/README.md"
echo ""
echo "4. Build and deploy the Orchestrator API:"
echo "   cd ../../src/OrchestratorAPI"
echo "   dotnet publish -c Release -o ./publish"
echo "   az webapp deploy \\"
echo "     --resource-group $RESOURCE_GROUP \\"
echo "     --name $APP_SERVICE_NAME \\"
echo "     --src-path ./publish \\"
echo "     --type zip"
echo ""

print_info "Deployment complete!"
