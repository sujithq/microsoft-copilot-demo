# Documentation Index

Welcome to the Microsoft Copilot GraphRAG Demo documentation. This index helps you find the right documentation for your needs.

## 🚀 Getting Started

- **[QUICKSTART.md](QUICKSTART.md)** - Get up and running in 15 minutes
- **[README.md](README.md)** - Complete project overview and architecture

## 🏗️ Architecture

- **[README.md - Architecture Section](README.md#architecture)** - High-level architecture with Mermaid diagrams
- **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** - Complete implementation summary
- **[AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md)** - Azure AI Foundry integration guide

## 🔧 MCP Server

- **[src/MCPServer/README.md](src/MCPServer/README.md)** - MCP Server comprehensive guide
- **[MCP_CONFIG.md](MCP_CONFIG.md)** - Configuration examples for various clients
- **[MCP_IMPLEMENTATION.md](MCP_IMPLEMENTATION.md)** - Technical implementation details

## 📋 Testing

- **[TESTING.md](TESTING.md)** - Complete testing guide
- **[scripts/test-mcp-server.sh](scripts/test-mcp-server.sh)** - MCP Server test script

## 🔐 Infrastructure

- **[infra/terraform/README.md](infra/terraform/README.md)** - Terraform deployment guide
- **[scripts/README.md](scripts/README.md)** - Data seeding and setup scripts

## 📊 Diagrams

All architecture diagrams use Mermaid format for better visualization:

### Main Architecture
- [Architecture Overview](README.md#architecture) - Complete system architecture
- [GraphRAG Workflow](README.md#orchestrator-workflow-graphrag--ai-search) - Sequence diagram of query processing

### MCP Server
- [MCP Communication](src/MCPServer/README.md#architecture) - MCP protocol flow
- [Integration Options](QUICKSTART.md#architecture-overview) - M365 Copilot integration paths
- [Tool Interaction](MCP_IMPLEMENTATION.md#integration-with-m365-copilot) - Detailed tool execution flow
- [Component Structure](MCP_IMPLEMENTATION.md#files-created) - MCP Server components

## 📖 By Role

### For Developers

Start here:
1. [QUICKSTART.md](QUICKSTART.md) - Setup instructions
2. [src/MCPServer/README.md](src/MCPServer/README.md) - MCP Server development
3. [TESTING.md](TESTING.md) - Testing procedures

### For Architects

Start here:
1. [README.md](README.md) - Architecture overview
2. [AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md) - Azure AI Foundry integration
3. [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Implementation details
4. [MCP_IMPLEMENTATION.md](MCP_IMPLEMENTATION.md) - Technical architecture

### For DevOps

Start here:
1. [infra/terraform/README.md](infra/terraform/README.md) - Infrastructure deployment
2. [QUICKSTART.md](QUICKSTART.md) - Deployment steps
3. [scripts/README.md](scripts/README.md) - Data seeding

### For End Users

Start here:
1. [MCP_CONFIG.md](MCP_CONFIG.md) - Client configuration
2. [src/MCPServer/README.md](src/MCPServer/README.md) - Available tools and usage

## 📁 Project Structure

```
microsoft-copilot-demo/
├── README.md                      ⭐ Start here
├── QUICKSTART.md                  🚀 15-minute setup
├── TESTING.md                     🧪 Testing guide
├── AZURE_AI_FOUNDRY.md            🤖 AI Foundry integration
├── IMPLEMENTATION_SUMMARY.md      📋 What was built
├── MCP_CONFIG.md                  ⚙️ MCP configuration
├── MCP_IMPLEMENTATION.md          🔧 MCP technical details
├── src/
│   ├── OrchestratorAPI/          🎯 GraphRAG backend
│   ├── CopilotAgent/             🤖 Bot Framework integration
│   └── MCPServer/                ⭐ MCP Server (NEW!)
│       └── README.md              📖 MCP Server guide
├── infra/
│   └── terraform/
│       └── README.md              🏗️ Infrastructure guide
└── scripts/
    ├── README.md                  📝 Data seeding guide
    └── test-mcp-server.sh         🧪 MCP test script
```

## 🎯 Common Tasks

### I want to...

#### Deploy the infrastructure
→ See [infra/terraform/README.md](infra/terraform/README.md)

#### Set up the MCP Server
→ See [src/MCPServer/README.md](src/MCPServer/README.md)

#### Configure for M365 Copilot
→ See [MCP_CONFIG.md](MCP_CONFIG.md)

#### Understand the architecture
→ See [README.md - Architecture](README.md#architecture) and [AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md)

#### Test the system
→ See [TESTING.md](TESTING.md)

#### Seed sample data
→ See [scripts/README.md](scripts/README.md)

#### Add new MCP tools
→ See [src/MCPServer/README.md - Development](src/MCPServer/README.md#development)

## 📞 Support

- 📖 Documentation: Check the relevant guide above
- 🐛 Issues: See [TESTING.md](TESTING.md) troubleshooting sections
- 💡 Questions: Review [MCP_IMPLEMENTATION.md](MCP_IMPLEMENTATION.md) for technical details

## 🔄 Update History

- **v1.1** (Feb 2026) - Added MCP Server with Mermaid diagrams
- **v1.0** (Feb 2026) - Initial GraphRAG implementation

---

**Quick Links:**
[README](README.md) | [Quick Start](QUICKSTART.md) | [MCP Server](src/MCPServer/README.md) | [Testing](TESTING.md) | [Infrastructure](infra/terraform/README.md)
