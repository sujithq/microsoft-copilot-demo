# GraphRAG Demo - Terraform Configuration
# 
# This configuration deploys a complete GraphRAG architecture using Microsoft Foundry.
# 
# IMPORTANT: Microsoft Foundry Hub-Project Architecture
# ======================================================
# Microsoft Foundry (formerly Azure AI Foundry) uses a hub-project model where:
# - Hub: Central management resource for AI projects (created via az cli)
# - Projects: Isolated workspaces under a hub (created via portal or SDK)
# 
# Since Terraform doesn't fully support Microsoft Foundry hub-project resources yet,
# this configuration creates the base AI Services account. Use the provision-foundry.sh
# script to create the full hub and projects via az cli.
#
# Resources created by Terraform:
# - Base AI Services account (acts as Foundry hub foundation)
# - Cosmos DB, AI Search, App Service (supporting infrastructure)
#
# Resources created by scripts/provision-foundry.sh:
# - Microsoft Foundry Hub with project management enabled
# - Microsoft Foundry Projects
# - Model deployments (gpt-5.2)

terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Variables
variable "environment" {
  description = "Environment name (dev, test, prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "eastus"
}

variable "name_prefix" {
  description = "Prefix for all resource names"
  type        = string
  default     = "graphrag"
}

variable "openai_deployment_name" {
  description = "Name of the OpenAI deployment"
  type        = string
  default     = "gpt-5.2"
}

# Random suffix for unique resource names
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

# Local variables
locals {
  resource_suffix     = random_string.suffix.result
  cosmos_account_name = "${var.name_prefix}-cosmos-${local.resource_suffix}"
  search_service_name = "${var.name_prefix}-search-${local.resource_suffix}"
  openai_account_name = "${var.name_prefix}-openai-${local.resource_suffix}"
  app_service_plan    = "${var.name_prefix}-asp-${local.resource_suffix}"
  app_service_name    = "${var.name_prefix}-api-${local.resource_suffix}"
  tags = {
    Environment = var.environment
    Project     = "GraphRAG-Demo"
    ManagedBy   = "Terraform"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${var.name_prefix}-${var.environment}-rg"
  location = var.location
  tags     = local.tags
}

# Cosmos DB Account
resource "azurerm_cosmosdb_account" "main" {
  name                = local.cosmos_account_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }

  tags = local.tags
}

# Cosmos DB SQL Database
resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "graphrag"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  throughput          = 400
}

# Cosmos DB Containers
resource "azurerm_cosmosdb_sql_container" "entities" {
  name                = "entities"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/id"]
  throughput          = 400
}

resource "azurerm_cosmosdb_sql_container" "relations" {
  name                = "relations"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/id"]
  throughput          = 400
}

resource "azurerm_cosmosdb_sql_container" "chunks" {
  name                = "chunks"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/id"]
  throughput          = 400
}

# Azure AI Search
resource "azurerm_search_service" "main" {
  name                = local.search_service_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "basic"
  replica_count       = 1
  partition_count     = 1

  tags = local.tags
}

# Microsoft Foundry Hub (Base AI Services Account)
# This creates the foundation for Microsoft Foundry Hub
# Note: Full hub features require additional provisioning via az cli or portal
# See scripts/provision-foundry.sh for complete hub setup
resource "azurerm_cognitive_account" "ai_services" {
  name                = local.openai_account_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  kind                = "AIServices"  # Multi-service account that forms the Foundry hub base
  sku_name            = "S0"

  custom_subdomain_name = local.openai_account_name

  tags = merge(local.tags, {
    Service = "Microsoft Foundry Hub"
    Platform = "Microsoft Foundry"
  })
}

# Alternative: Keep OpenAI-specific account for backwards compatibility
# Uncomment if you need just OpenAI instead of full AI Services
# resource "azurerm_cognitive_account" "openai" {
#   name                = local.openai_account_name
#   location            = azurerm_resource_group.main.location
#   resource_group_name = azurerm_resource_group.main.name
#   kind                = "OpenAI"
#   sku_name            = "S0"
#
#   custom_subdomain_name = local.openai_account_name
#
#   tags = local.tags
# }

# App Service Plan
resource "azurerm_service_plan" "main" {
  name                = local.app_service_plan
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = "B1"

  tags = local.tags
}

# App Service for Orchestrator API
resource "azurerm_linux_web_app" "main" {
  name                = local.app_service_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = {
    "CosmosDb__Endpoint"           = azurerm_cosmosdb_account.main.endpoint
    "CosmosDb__DatabaseId"         = azurerm_cosmosdb_sql_database.main.name
    "CosmosDb__EntitiesContainerId" = azurerm_cosmosdb_sql_container.entities.name
    "CosmosDb__RelationsContainerId" = azurerm_cosmosdb_sql_container.relations.name
    "CosmosDb__ChunksContainerId"   = azurerm_cosmosdb_sql_container.chunks.name
    "AzureSearch__Endpoint"        = "https://${azurerm_search_service.main.name}.search.windows.net"
    "AzureSearch__IndexName"       = "chunks"
    "AzureOpenAI__Endpoint"        = azurerm_cognitive_account.ai_services.endpoint
    "AzureOpenAI__DeploymentName"  = var.openai_deployment_name
  }

  tags = local.tags
}

# Role Assignment: Cosmos DB Data Contributor for App Service
resource "azurerm_cosmosdb_sql_role_assignment" "app_service" {
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  role_definition_id  = "${azurerm_cosmosdb_account.main.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002"
  principal_id        = azurerm_linux_web_app.main.identity[0].principal_id
  scope               = azurerm_cosmosdb_account.main.id
}

# Role Assignment: Cognitive Services OpenAI User for App Service
resource "azurerm_role_assignment" "openai_user" {
  scope                = azurerm_cognitive_account.ai_services.id
  role_definition_name = "Cognitive Services OpenAI User"
  principal_id         = azurerm_linux_web_app.main.identity[0].principal_id
}

# Role Assignment: Search Index Data Contributor for App Service
resource "azurerm_role_assignment" "search_contributor" {
  scope                = azurerm_search_service.main.id
  role_definition_name = "Search Index Data Contributor"
  principal_id         = azurerm_linux_web_app.main.identity[0].principal_id
}

# Outputs
output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "cosmos_endpoint" {
  description = "Cosmos DB endpoint"
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "cosmos_primary_key" {
  description = "Cosmos DB primary key (sensitive)"
  value       = azurerm_cosmosdb_account.main.primary_key
  sensitive   = true
}

output "search_endpoint" {
  description = "Azure AI Search endpoint"
  value       = "https://${azurerm_search_service.main.name}.search.windows.net"
}

output "search_primary_key" {
  description = "Azure AI Search primary admin key (sensitive)"
  value       = azurerm_search_service.main.primary_key
  sensitive   = true
}

output "openai_endpoint" {
  description = "Microsoft Foundry Hub (AI Services) endpoint"
  value       = azurerm_cognitive_account.ai_services.endpoint
}

output "foundry_hub_name" {
  description = "Microsoft Foundry Hub name"
  value       = azurerm_cognitive_account.ai_services.name
}

output "app_service_url" {
  description = "App Service URL"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "app_service_name" {
  description = "App Service name"
  value       = azurerm_linux_web_app.main.name
}

output "app_service_principal_id" {
  description = "App Service Managed Identity Principal ID"
  value       = azurerm_linux_web_app.main.identity[0].principal_id
}
