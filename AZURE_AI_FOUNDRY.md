# Azure AI Foundry Integration

## What is Azure AI Foundry?

**Azure AI Foundry** (formerly known as Azure AI Studio) is Microsoft's unified AI platform that provides:

- **Centralized AI Services**: Single account for multiple AI capabilities
- **Azure OpenAI Models**: GPT-4, GPT-5.2, embeddings, and more
- **Content Safety**: Built-in content filtering and safety features
- **Model Management**: Deploy, manage, and monitor AI models
- **Unified Billing**: Single billing for all AI services

## Why Azure AI Foundry?

Azure AI Foundry represents the evolution from standalone Azure OpenAI to a comprehensive AI platform:

| Feature | Azure OpenAI (Old) | Azure AI Foundry (New) |
|---------|-------------------|----------------------|
| **Account Type** | OpenAI-specific | Multi-service (AIServices kind) |
| **Capabilities** | OpenAI models only | OpenAI + Content Safety + more |
| **Management** | Separate accounts | Unified platform |
| **Future-Proof** | Legacy approach | Modern, extensible |
| **Recommended** | No | ✅ Yes |

## Implementation in This Project

### Infrastructure (Terraform)

The infrastructure uses **Azure AI Services** (kind: `AIServices`) instead of the older OpenAI-only account:

```hcl
# Azure AI Foundry (Azure AI Services with OpenAI)
resource "azurerm_cognitive_account" "ai_services" {
  name  = local.openai_account_name
  kind  = "AIServices"  # Multi-service account
  # ... includes Azure OpenAI, Content Safety, etc.
}
```

**Benefits:**
- ✅ Access to all Azure AI services through one account
- ✅ Future-proof as new AI services are added
- ✅ Unified authentication and billing
- ✅ Compatible with existing Azure.AI.OpenAI SDK

### Code Integration

The .NET code uses `Azure.AI.OpenAI` SDK which works seamlessly with Azure AI Foundry:

```csharp
// Configure Azure AI Foundry (Azure AI Services with OpenAI)
builder.Services.AddSingleton(sp =>
{
    var endpoint = config["AzureOpenAI:Endpoint"];
    var credential = new DefaultAzureCredential();
    return new AzureOpenAIClient(new Uri(endpoint), credential);
});
```

The SDK automatically handles:
- ✅ Authentication to AI Services account
- ✅ Model deployment access (gpt-5.2)
- ✅ API versioning and compatibility

### Configuration

**appsettings.json:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-ai-services.openai.azure.com/",
    "DeploymentName": "gpt-5.2"
  }
}
```

**Note**: The endpoint URL format is the same whether using AI Services or OpenAI-only accounts.

## Deployment

### 1. Create AI Services Account

Terraform automatically creates an AI Services account:

```bash
cd infra/terraform
terraform apply
```

This creates a resource with `kind = "AIServices"` which includes:
- Azure OpenAI capabilities
- Content Safety
- Other Azure AI services

### 2. Deploy gpt-5.2 Model

After infrastructure is created:

```bash
AI_SERVICES_ACCOUNT="<from terraform output>"
RESOURCE_GROUP="<from terraform output>"

az cognitiveservices account deployment create \
  --name $AI_SERVICES_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --deployment-name gpt-5.2 \
  --model-name gpt-5.2 \
  --model-version latest \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard
```

## Model Capabilities

### gpt-5.2 (Chat Completion)

Used for answer generation in the GraphRAG pipeline:

- **Input**: System prompt + user context + question
- **Output**: Comprehensive answer with reasoning
- **Features**: Citations, structured responses, context understanding

### text-embedding-ada-002 (Optional)

Can be deployed for generating embeddings:

```bash
az cognitiveservices account deployment create \
  --name $AI_SERVICES_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --deployment-name text-embedding-ada-002 \
  --model-name text-embedding-ada-002 \
  --model-version "2" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name "Standard"
```

## Security & Access

### Managed Identity

The App Service uses System-Assigned Managed Identity with role:

```hcl
resource "azurerm_role_assignment" "openai_user" {
  scope                = azurerm_cognitive_account.ai_services.id
  role_definition_name = "Cognitive Services OpenAI User"
  principal_id         = azurerm_linux_web_app.main.identity[0].principal_id
}
```

**Permissions granted:**
- ✅ Access to deployed models (gpt-5.2, embeddings, etc.)
- ✅ Make inference requests
- ✅ List deployments

### No API Keys Required

With Managed Identity:
- ✅ No keys in configuration
- ✅ Automatic token acquisition
- ✅ Rotation handled by Azure
- ✅ Audit trail in Azure AD

## Migration from Azure OpenAI

If you have existing Azure OpenAI resources, migration is straightforward:

### Option 1: Create New AI Services Account (Recommended)

1. Deploy new AI Services account via Terraform
2. Migrate model deployments to new account
3. Update application configuration
4. Delete old OpenAI account

### Option 2: Keep Existing OpenAI Account

Uncomment the alternative configuration in `main.tf`:

```hcl
# Alternative: Keep OpenAI-specific account
resource "azurerm_cognitive_account" "openai" {
  kind = "OpenAI"  # OpenAI-only account
  # ...
}
```

Update references from `ai_services` to `openai`.

## Monitoring & Costs

### Azure Monitor Integration

AI Services accounts integrate with Azure Monitor:

- **Metrics**: Token usage, latency, errors
- **Logs**: Request/response logging
- **Alerts**: Set up alerts for quota, errors, or latency

### Cost Optimization

- **Token-based pricing**: Pay only for what you use
- **Shared capacity**: Multiple models in one account
- **Quota management**: Set deployment capacity limits

Typical costs for demo:
- gpt-5.2: $0.03 per 1K tokens (input) / $0.06 per 1K tokens (output)
- Embeddings: $0.0001 per 1K tokens

## References

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Services](https://learn.microsoft.com/azure/ai-services/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Terraform azurerm_cognitive_account](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/cognitive_account)

## Summary

This project now uses **Azure AI Foundry** through Azure AI Services accounts:

✅ **Infrastructure**: Terraform creates `AIServices` kind accounts  
✅ **Code**: Azure.AI.OpenAI SDK with latest versions  
✅ **Security**: Managed Identity with proper RBAC  
✅ **Models**: gpt-5.2 for chat completion  
✅ **Future-Ready**: Extensible to new AI services  

The implementation follows Microsoft's latest best practices for AI application development.
