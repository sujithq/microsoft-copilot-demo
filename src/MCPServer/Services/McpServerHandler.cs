using System.Text.Json;
using MCPServer.Protocol;
using Microsoft.Extensions.Logging;

namespace MCPServer.Services;

/// <summary>
/// Handles MCP protocol messages and dispatches to appropriate handlers
/// </summary>
public class McpServerHandler
{
    private readonly McpToolService _toolService;
    private readonly ILogger<McpServerHandler> _logger;
    private bool _initialized = false;

    public McpServerHandler(McpToolService toolService, ILogger<McpServerHandler> logger)
    {
        _toolService = toolService;
        _logger = logger;
    }

    /// <summary>
    /// Process an MCP request and return a response
    /// </summary>
    public async Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling MCP request: {Method} (id: {Id})", request.Method, request.Id);

        try
        {
            var result = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleListTools(request),
                "tools/call" => await HandleToolCallAsync(request, cancellationToken),
                "ping" => HandlePing(request),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request: {Method}", request.Method);
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message
                }
            };
        }
    }

    private object HandleInitialize(McpRequest request)
    {
        _logger.LogInformation("Initializing MCP server");
        _initialized = true;

        return new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability
                {
                    ListChanged = false
                }
            },
            ServerInfo = new ServerInfo
            {
                Name = "GraphRAG MCP Server",
                Version = "1.0.0"
            }
        };
    }

    private object HandleListTools(McpRequest request)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Server not initialized. Call 'initialize' first.");
        }

        _logger.LogInformation("Listing available tools");
        var tools = _toolService.GetTools();

        return new ListToolsResult
        {
            Tools = tools
        };
    }

    private async Task<object> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Server not initialized. Call 'initialize' first.");
        }

        if (request.Params == null)
        {
            throw new ArgumentException("Tool call requires params");
        }

        // Deserialize params to ToolCallParams
        var paramsJson = JsonSerializer.Serialize(request.Params);
        var toolCall = JsonSerializer.Deserialize<ToolCallParams>(paramsJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (toolCall == null)
        {
            throw new InvalidOperationException("Failed to deserialize tool call params");
        }

        _logger.LogInformation("Calling tool: {ToolName}", toolCall.Name);
        var result = await _toolService.ExecuteToolAsync(toolCall, cancellationToken);

        return result;
    }

    private object HandlePing(McpRequest request)
    {
        _logger.LogDebug("Ping received");
        return new { status = "ok" };
    }
}
