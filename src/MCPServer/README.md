# GraphRAG MCP Server

A Model Context Protocol (MCP) server that provides Microsoft 365 Copilot with access to the GraphRAG knowledge base through standardized tools.

## What is MCP?

Model Context Protocol (MCP) is an open protocol that enables AI assistants to securely connect to various data sources and tools. This MCP server exposes the GraphRAG orchestrator's capabilities through MCP-compliant tools that can be called by Microsoft 365 Copilot or other MCP clients.

## Architecture

```
M365 Copilot → MCP Server (stdio) → Orchestrator API → GraphRAG Backend
```

The MCP server acts as a bridge, translating MCP tool calls into Orchestrator API requests.

## Available Tools

### 1. `graphrag_query`

Query the GraphRAG knowledge base using the full GraphRAG pipeline:
- Entity linking
- Graph expansion
- Hybrid retrieval (BM25 + vector)
- Answer generation with citations

**Parameters:**
- `query` (required): The question to ask
- `user_id` (optional): User identifier
- `conversation_id` (optional): Conversation context ID

**Example:**
```json
{
  "name": "graphrag_query",
  "arguments": {
    "query": "If Service A fails, what breaks and who owns escalation?"
  }
}
```

### 2. `entity_lookup`

Look up detailed information about a specific entity in the knowledge graph.

**Parameters:**
- `entity_id` (required): The entity identifier (e.g., `service-a`, `team-y`)

**Example:**
```json
{
  "name": "entity_lookup",
  "arguments": {
    "entity_id": "service-a"
  }
}
```

### 3. `graph_expansion`

Expand the knowledge graph from a starting entity to discover relationships.

**Parameters:**
- `entity_id` (required): Starting entity identifier
- `hops` (optional): Number of relationship hops (1-3, default: 1)

**Example:**
```json
{
  "name": "graph_expansion",
  "arguments": {
    "entity_id": "service-a",
    "hops": 2
  }
}
```

## Configuration

Edit `appsettings.json` to configure the Orchestrator API endpoint:

```json
{
  "OrchestratorApi": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

Or set via environment variable:
```bash
export OrchestratorApi__BaseUrl="https://your-orchestrator.azurewebsites.net"
```

## Running the Server

### Standalone Mode (stdio)

The MCP server communicates via JSON-RPC over stdio:

```bash
dotnet run --project src/MCPServer
```

The server reads JSON-RPC requests from stdin and writes responses to stdout.

### With MCP Client

Use an MCP client (like Claude Desktop) to connect:

1. Add to your MCP client configuration:

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/MCPServer"]
    }
  }
}
```

2. The client will automatically start the server and communicate via stdio.

### Testing with curl

You can test the server by piping JSON-RPC messages:

```bash
# Initialize
echo '{"jsonrpc":"2.0","id":"1","method":"initialize","params":{}}' | dotnet run --project src/MCPServer

# List tools
echo '{"jsonrpc":"2.0","id":"2","method":"tools/list","params":{}}' | dotnet run --project src/MCPServer

# Call graphrag_query
echo '{"jsonrpc":"2.0","id":"3","method":"tools/call","params":{"name":"graphrag_query","arguments":{"query":"What is Service A?"}}}' | dotnet run --project src/MCPServer
```

## Protocol Details

### JSON-RPC Messages

All messages follow JSON-RPC 2.0 format:

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": "unique-id",
  "method": "method-name",
  "params": {}
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "unique-id",
  "result": {}
}
```

**Error Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "unique-id",
  "error": {
    "code": -32603,
    "message": "Error description"
  }
}
```

### Supported Methods

- `initialize`: Initialize the server and get capabilities
- `tools/list`: List all available tools
- `tools/call`: Execute a tool
- `ping`: Health check

## Integration with M365 Copilot

To use this MCP server with Microsoft 365 Copilot:

1. Deploy the MCP server to a secure environment
2. Configure M365 Copilot to connect to the MCP server
3. Grant appropriate permissions for Copilot to invoke the tools
4. Users can now ask questions that leverage the GraphRAG knowledge base

Example Copilot interactions:

```
User: "If Service A fails, what breaks?"
Copilot: [Calls graphrag_query tool] → Returns answer with citations

User: "Show me everything related to Team Y"
Copilot: [Calls graph_expansion tool] → Returns related entities and relationships
```

## Development

### Project Structure

```
src/MCPServer/
├── Models/
│   └── OrchestratorModels.cs     # Data models
├── Protocol/
│   ├── McpMessage.cs              # JSON-RPC message types
│   ├── McpTool.cs                 # Tool definitions
│   └── McpTypes.cs                # MCP protocol types
├── Services/
│   ├── OrchestratorClient.cs     # HTTP client for Orchestrator API
│   ├── McpToolService.cs         # Tool implementations
│   └── McpServerHandler.cs       # JSON-RPC request handler
├── Program.cs                     # Main entry point (stdio server)
└── appsettings.json              # Configuration
```

### Adding New Tools

1. Add tool definition in `McpToolService.InitializeTools()`:
```csharp
new McpTool
{
    Name = "my_tool",
    Description = "Tool description",
    InputSchema = new McpToolInputSchema { ... }
}
```

2. Add handler in `McpToolService.ExecuteToolAsync()`:
```csharp
"my_tool" => await ExecuteMyToolAsync(toolCall.Arguments, cancellationToken),
```

3. Implement the tool method:
```csharp
private async Task<string> ExecuteMyToolAsync(
    Dictionary<string, object> arguments, 
    CancellationToken cancellationToken)
{
    // Implementation
}
```

## Security Considerations

- The MCP server should run in a trusted environment
- Configure authentication if deploying remotely
- Use HTTPS for Orchestrator API communication
- Implement rate limiting for production deployments
- Validate all tool inputs before processing

## Troubleshooting

### Server not starting

Check that the Orchestrator API is accessible:
```bash
curl http://localhost:5000/api/health
```

### Connection errors

Verify the `OrchestratorApi:BaseUrl` in `appsettings.json` is correct.

### Tool execution failures

Enable detailed logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Resources

- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [MCP SDK Documentation](https://github.com/modelcontextprotocol)
- [Microsoft 365 Copilot Documentation](https://learn.microsoft.com/microsoft-365-copilot/)

## License

MIT License - see LICENSE file for details
