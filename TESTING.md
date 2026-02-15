# Testing Guide for GraphRAG Demo

This guide covers testing the GraphRAG demo implementation, including the Orchestrator API, Copilot Agent, and MCP Server.

## Local Testing

### 1. Build Verification

```bash
# Build the solution
cd /home/runner/work/microsoft-copilot-demo/microsoft-copilot-demo
dotnet build

# Expected output: Build succeeded with 0 warnings and 0 errors
```

### 2. Run Orchestrator API Locally

```bash
cd src/OrchestratorAPI
dotnet run
```

The API will start on `http://localhost:5000` (or the port specified in launchSettings.json).

### 3. Test Health Endpoint

```bash
curl http://localhost:5000/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-14T06:00:00.0000000Z"
}
```

### 4. Test Ask Endpoint

**Note**: This requires properly configured Azure resources (Cosmos DB, AI Search, Azure AI Foundry).

```bash
curl -X POST http://localhost:5000/api/ask \
  -H "Content-Type: application/json" \
  -d '{
    "user": {
      "aadObjectId": "test-user-123"
    },
    "conversationId": "test-conversation-456",
    "query": "If Service A fails, what breaks and who owns escalation?",
    "context": {
      "tenantId": "test-tenant-789",
      "locale": "en-US"
    }
  }'
```

Expected response (with configured backend):
```json
{
  "answer": "Based on the documentation...",
  "citations": [
    {
      "title": "Service A Runbook",
      "url": "https://docs.example.com/...",
      "chunkId": "doc1#chunk1"
    }
  ],
  "trace": {
    "linkedEntities": ["service-a"],
    "expandedEntityIds": ["service-a", "team-y"],
    "searchFilter": "entityIds/any(e: e eq 'service-a' or e eq 'team-y')"
  }
}
```

### 5. Test MCP Server

The MCP Server provides a Model Context Protocol interface for M365 Copilot.

#### Start MCP Server

```bash
cd src/MCPServer
export OrchestratorApi__BaseUrl="http://localhost:5000"
dotnet run
```

The server reads JSON-RPC messages from stdin and writes responses to stdout.

#### Test Initialize Method

```bash
echo '{"jsonrpc":"2.0","id":"1","method":"initialize","params":{}}' | \
  dotnet run --project src/MCPServer
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {
        "listChanged": false
      }
    },
    "serverInfo": {
      "name": "GraphRAG MCP Server",
      "version": "1.0.0"
    }
  }
}
```

#### Test List Tools

```bash
echo '{"jsonrpc":"2.0","id":"2","method":"tools/list","params":{}}' | \
  dotnet run --project src/MCPServer
```

Expected: Returns list of 3 tools (graphrag_query, entity_lookup, graph_expansion)

#### Test Tool Call (requires Orchestrator API running)

```bash
echo '{"jsonrpc":"2.0","id":"3","method":"tools/call","params":{"name":"graphrag_query","arguments":{"query":"What is Service A?"}}}' | \
  dotnet run --project src/MCPServer
```

Expected: Returns tool result with answer and citations

#### Run MCP Test Script

```bash
./scripts/test-mcp-server.sh
```

This script tests:
- Server initialization
- Tool listing
- Ping endpoint
```

## Integration Testing

### Prerequisites

1. Azure resources deployed via Terraform
2. Azure AI Search index created
3. Sample data loaded into Cosmos DB and AI Search
4. Azure AI Foundry gpt-5.2 model deployed

### Test Workflow

1. **Entity Linking Test**
   - Query: "What is Service A?"
   - Expected: Links to "service-a" entity

2. **Graph Expansion Test**
   - Query: "What does Service A depend on?"
   - Expected: Expands to include "process-x" and related entities

3. **Hybrid Retrieval Test**
   - Query: "Service A documentation"
   - Expected: Returns relevant chunks from AI Search

4. **Answer Generation Test**
   - Query: "Who do I contact if Service A fails?"
   - Expected: Returns answer with citation to Team Y contact info

## Docker Testing

### Build Docker Image

```bash
cd src/OrchestratorAPI
docker build -t orchestrator-api:latest .
```

### Run Container

```bash
docker run -p 5000:8080 \
  -e CosmosDb__Endpoint="https://your-cosmos.documents.azure.com:443/" \
  -e AzureSearch__Endpoint="https://your-search.search.windows.net" \
  -e AzureOpenAI__Endpoint="https://your-openai.openai.azure.com/" \
  -e AzureOpenAI__DeploymentName="gpt-5.2" \
  orchestrator-api:latest
```

### Test Container

```bash
curl http://localhost:5000/api/health
```

## Terraform Testing

### Validate Configuration

```bash
cd infra/terraform
terraform init
terraform validate
```

Expected output:
```
Success! The configuration is valid.
```

### Plan Deployment

```bash
terraform plan
```

Review the planned changes to ensure all resources are correctly configured.

### Test Variables

```bash
# Test with custom variables
terraform plan -var="environment=test" -var="location=westus2"
```

## Load Testing

### Using Apache Bench

```bash
# Test health endpoint
ab -n 1000 -c 10 http://localhost:5000/api/health

# Test ask endpoint (requires JSON payload file)
ab -n 100 -c 5 -p test-request.json -T application/json \
  http://localhost:5000/api/ask
```

### Expected Performance

- Health endpoint: < 10ms response time
- Ask endpoint: 500ms - 2s response time (depends on AI models)

## Security Testing

### 1. Authentication Test

Verify that the API properly handles authentication:

```bash
# Should succeed with valid Managed Identity or credentials
curl http://localhost:5000/api/ask -H "Authorization: Bearer <token>"

# Should fail without proper authentication in production
curl http://localhost:5000/api/ask
```

### 2. Input Validation Test

Test with malformed requests:

```bash
# Missing required fields
curl -X POST http://localhost:5000/api/ask \
  -H "Content-Type: application/json" \
  -d '{"query": "test"}'

# Expected: 400 Bad Request
```

### 3. CORS Test

If CORS is configured:

```bash
curl -H "Origin: https://example.com" \
  -H "Access-Control-Request-Method: POST" \
  -X OPTIONS http://localhost:5000/api/ask
```

## Monitoring and Logging

### Check Application Logs

```bash
# View logs in development
dotnet run --verbosity normal

# View logs in Docker
docker logs <container-id>

# View logs in Azure App Service
az webapp log tail --name <app-name> --resource-group <rg-name>
```

### Expected Log Entries

- Entity linking results
- Graph expansion metrics
- Search query performance
- Answer generation timing

## Troubleshooting

### Common Issues

1. **Connection Errors to Azure Services**
   - Check: Managed Identity permissions
   - Check: Network connectivity
   - Check: Endpoint URLs in configuration

2. **Empty Search Results**
   - Check: AI Search index exists
   - Check: Sample data loaded
   - Check: Entity IDs match between Cosmos and Search

3. **OpenAI API Errors**
   - Check: gpt-5.2 deployment exists in Azure AI Foundry
   - Check: Quota and rate limits
   - Check: Endpoint and credentials

4. **Build Errors**
   - Check: .NET 10 SDK installed
   - Check: All NuGet packages restored
   - Run: `dotnet restore`

## Automated Testing

### Unit Tests (Future Enhancement)

Create unit tests for each service:

```csharp
[Fact]
public async Task EntityLinkingService_ShouldLinkEntities()
{
    // Arrange
    var service = new EntityLinkingService(...);
    
    // Act
    var entities = await service.LinkEntitiesAsync("Service A");
    
    // Assert
    Assert.Contains("service-a", entities);
}
```

### Integration Tests (Future Enhancement)

Create integration tests for the full workflow:

```csharp
[Fact]
public async Task OrchestratorService_ShouldReturnAnswer()
{
    // Arrange
    var request = new AskRequest { ... };
    
    // Act
    var response = await orchestrator.ProcessQueryAsync(request);
    
    // Assert
    Assert.NotEmpty(response.Answer);
    Assert.NotEmpty(response.Citations);
}
```

## CI/CD Testing

### GitHub Actions Workflow (Future Enhancement)

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

## Success Criteria

- ✅ All projects build successfully
- ✅ Health endpoint returns 200 OK
- ✅ Ask endpoint accepts valid requests
- ✅ Terraform validates without errors
- ✅ Docker image builds successfully
- ✅ Application logs show expected workflow steps
- ✅ Integration with Azure services works (when configured)

## Next Steps

1. Deploy infrastructure using Terraform
2. Seed sample data
3. Test end-to-end workflow with real Azure resources
4. Integrate with Microsoft 365 Copilot
5. Add comprehensive unit and integration tests
