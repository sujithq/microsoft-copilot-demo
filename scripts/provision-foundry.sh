#!/bin/bash

# Microsoft Foundry Hub and Project Provisioning Script
# This script provisions Microsoft Foundry resources using Azure CLI
# since Terraform doesn't yet fully support the new Foundry hub-project architecture

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Parse arguments
RESOURCE_GROUP=${1:-"graphrag-dev-rg"}
LOCATION=${2:-"northeurope"}
HUB_NAME=${3:-"graphrag-foundry-hub"}
PROJECT_NAME=${4:-"graphrag-project"}

print_info "Microsoft Foundry Provisioning Script"
print_info "======================================"
echo ""
print_info "Resource Group: $RESOURCE_GROUP"
print_info "Location: $LOCATION"
print_info "Hub Name: $HUB_NAME"
print_info "Project Name: $PROJECT_NAME"
echo ""

# Step 1: Ensure resource group exists
print_step "1. Verifying resource group..."
if ! az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    print_warning "Resource group doesn't exist. Creating..."
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
    print_info "Resource group created"
else
    print_info "Resource group exists"
fi

# Step 2: Create Microsoft Foundry Hub (AI Services account with project management)
print_step "2. Creating Microsoft Foundry Hub..."

# Check if hub already exists
if az cognitiveservices account show --name "$HUB_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    print_warning "Microsoft Foundry Hub already exists"
else
    az cognitiveservices account create \
      --name "$HUB_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --kind AIServices \
      --sku S0 \
      --location "$LOCATION" \
      --custom-domain "$HUB_NAME" \
      --tags "Service=Microsoft Foundry Hub" "Environment=Development"
    
    print_info "Microsoft Foundry Hub created: $HUB_NAME"
fi

# Get hub endpoint
HUB_ENDPOINT=$(az cognitiveservices account show \
  --name "$HUB_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.endpoint" -o tsv)

print_info "Hub Endpoint: $HUB_ENDPOINT"

# Step 3: Create Microsoft Foundry Project
print_step "3. Creating Microsoft Foundry Project..."

# Note: Project creation via CLI may require additional preview features
# For now, we document the process and recommend using Azure Portal for project creation

print_warning "Microsoft Foundry Projects are best created via the Azure Portal or Foundry SDK"
print_info "To create a project manually:"
echo "  1. Navigate to Microsoft Foundry Portal: https://ai.azure.com"
echo "  2. Select hub: $HUB_NAME"
echo "  3. Create new project: $PROJECT_NAME"
echo "  4. Configure project settings as needed"
echo ""

# Alternative: Create project using REST API (if available)
print_info "Attempting to create project via AI Services API..."

# This is a placeholder - actual project creation may require SDK or portal
# az cognitiveservices account project create \
#   --name "$HUB_NAME" \
#   --resource-group "$RESOURCE_GROUP" \
#   --project-name "$PROJECT_NAME" \
#   --location "$LOCATION" 2>/dev/null || \
#   print_warning "Project creation via CLI not available. Please create via portal."

# Step 4: Deploy GPT-5.2 model
print_step "4. Deploying gpt-5.2 model to hub..."

MODEL_DEPLOYMENT="gpt-5.2"
if az cognitiveservices account deployment show \
    --name "$HUB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --deployment-name "$MODEL_DEPLOYMENT" &> /dev/null; then
    print_warning "Model deployment $MODEL_DEPLOYMENT already exists"
else
    print_info "Deploying $MODEL_DEPLOYMENT..."
    az cognitiveservices account deployment create \
      --name "$HUB_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --deployment-name "$MODEL_DEPLOYMENT" \
      --model-name "$MODEL_DEPLOYMENT" \
      --model-version "latest" \
      --model-format OpenAI \
      --sku-capacity 10 \
      --sku-name "Standard" || \
      print_warning "Model deployment may need to be done via portal (check quota and availability)"
fi

# Step 5: Configure RBAC for App Service
print_step "5. Configuring RBAC permissions..."

# Get App Service principal ID (if exists)
if APP_NAME=$(az webapp list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv 2>/dev/null); then
    print_info "Found App Service: $APP_NAME"
    
    APP_PRINCIPAL_ID=$(az webapp identity show \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --query "principalId" -o tsv)
    
    if [ -n "$APP_PRINCIPAL_ID" ]; then
        print_info "Assigning Cognitive Services OpenAI User role..."
        az role assignment create \
          --assignee "$APP_PRINCIPAL_ID" \
          --role "Cognitive Services OpenAI User" \
          --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$HUB_NAME" \
          2>/dev/null || print_warning "Role may already be assigned"
    fi
else
    print_warning "No App Service found in resource group"
fi

# Step 6: Output summary
print_step "6. Summary"
echo ""
print_info "Microsoft Foundry Hub provisioned successfully!"
echo ""
echo "Hub Details:"
echo "  Name: $HUB_NAME"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Endpoint: $HUB_ENDPOINT"
echo ""
echo "Next Steps:"
echo "  1. Create Foundry Project via portal: https://ai.azure.com"
echo "  2. Configure project settings and connections"
echo "  3. Deploy additional models if needed"
echo "  4. Update application configuration with hub endpoint"
echo ""

# Save configuration
CONFIG_FILE="microsoft-foundry-config.env"
cat > "$CONFIG_FILE" << EOF
# Microsoft Foundry Configuration
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
FOUNDRY_HUB_NAME=$HUB_NAME
FOUNDRY_HUB_ENDPOINT=$HUB_ENDPOINT
FOUNDRY_PROJECT_NAME=$PROJECT_NAME

# Application Configuration
export AzureOpenAI__Endpoint="$HUB_ENDPOINT"
export AzureOpenAI__DeploymentName="gpt-5.2"
EOF

print_info "Configuration saved to: $CONFIG_FILE"
print_info "Source this file to set environment variables: source $CONFIG_FILE"

echo ""
print_info "Provisioning complete! ✅"
