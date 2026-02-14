# MCP Configuration Examples

This file contains example configurations for connecting MCP clients to the GraphRAG MCP Server.

## Claude Desktop Configuration

Add this to your Claude Desktop MCP configuration file:

### macOS/Linux
Location: `~/Library/Application Support/Claude/claude_desktop_config.json`

### Windows
Location: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/microsoft-copilot-demo/src/MCPServer"
      ],
      "env": {
        "OrchestratorApi__BaseUrl": "http://localhost:5000"
      }
    }
  }
}
```

### For Deployed Orchestrator

If your Orchestrator API is deployed to Azure:

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/microsoft-copilot-demo/src/MCPServer"
      ],
      "env": {
        "OrchestratorApi__BaseUrl": "https://your-orchestrator.azurewebsites.net"
      }
    }
  }
}
```

## Microsoft 365 Copilot Configuration

For M365 Copilot integration, configure the MCP server as an external tool:

```json
{
  "tools": {
    "graphrag": {
      "type": "mcp",
      "config": {
        "command": "dotnet",
        "args": [
          "run",
          "--project",
          "/path/to/src/MCPServer"
        ],
        "env": {
          "OrchestratorApi__BaseUrl": "https://your-orchestrator.azurewebsites.net"
        }
      }
    }
  }
}
```

## Using Published Executable

For production deployments, publish the MCP server as a self-contained executable:

```bash
cd src/MCPServer
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

Then configure:

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "/path/to/publish/MCPServer.exe",
      "env": {
        "OrchestratorApi__BaseUrl": "https://your-orchestrator.azurewebsites.net"
      }
    }
  }
}
```

### Linux/macOS

```bash
cd src/MCPServer
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
chmod +x ./publish/MCPServer
```

Configuration:

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "/path/to/publish/MCPServer",
      "env": {
        "OrchestratorApi__BaseUrl": "https://your-orchestrator.azurewebsites.net"
      }
    }
  }
}
```

## Testing the Configuration

After adding the configuration:

1. Restart your MCP client (e.g., Claude Desktop)
2. The GraphRAG tools should appear in the tool list
3. Test with a query like: "What is Service A?"

The client will automatically:
- Start the MCP server process
- Connect via stdio
- Make the tools available to the AI assistant

## Available Tools

Once configured, you'll have access to:

1. **graphrag_query**: Ask questions with full GraphRAG pipeline
   - Example: "If Service A fails, what breaks and who owns escalation?"

2. **entity_lookup**: Get information about specific entities
   - Example: "Look up service-a"

3. **graph_expansion**: Discover related entities
   - Example: "Show me everything related to team-y"

## Troubleshooting

### MCP Server not starting

Check logs in your MCP client's console/logs directory.

Common issues:
- Incorrect path to project
- .NET 10 SDK not installed
- Orchestrator API not accessible

### Tools not appearing

Verify the MCP server is running by checking the client's tool list. The server should initialize and list 3 tools.

### Tool execution fails

Ensure:
1. Orchestrator API is running and accessible
2. `OrchestratorApi__BaseUrl` is correctly configured
3. Network connectivity between MCP server and Orchestrator

## Security Considerations

For production:
- Deploy Orchestrator API with authentication
- Use HTTPS for all connections
- Implement rate limiting
- Run MCP server in a secure environment
- Use managed identities for Azure resources

## Advanced Configuration

### Custom Logging

Set environment variables for logging:

```json
{
  "mcpServers": {
    "graphrag": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MCPServer"],
      "env": {
        "OrchestratorApi__BaseUrl": "https://your-orchestrator.azurewebsites.net",
        "Logging__LogLevel__Default": "Debug"
      }
    }
  }
}
```

### Multiple Environments

Configure different servers for dev/staging/prod:

```json
{
  "mcpServers": {
    "graphrag-dev": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MCPServer"],
      "env": {
        "OrchestratorApi__BaseUrl": "http://localhost:5000"
      }
    },
    "graphrag-prod": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MCPServer"],
      "env": {
        "OrchestratorApi__BaseUrl": "https://prod-orchestrator.azurewebsites.net"
      }
    }
  }
}
```
