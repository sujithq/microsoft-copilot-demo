using MCPServer.Protocol;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MCPServer.Services;

/// <summary>
/// Service for managing MCP tools
/// </summary>
public class McpToolService
{
    private readonly OrchestratorClient _orchestratorClient;
    private readonly ILogger<McpToolService> _logger;
    private readonly List<McpTool> _tools;

    public McpToolService(OrchestratorClient orchestratorClient, ILogger<McpToolService> logger)
    {
        _orchestratorClient = orchestratorClient;
        _logger = logger;
        _tools = InitializeTools();
    }

    /// <summary>
    /// Get all available tools
    /// </summary>
    public List<McpTool> GetTools() => _tools;

    /// <summary>
    /// Execute a tool call
    /// </summary>
    public async Task<ToolCallResult> ExecuteToolAsync(ToolCallParams toolCall, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing tool: {ToolName}", toolCall.Name);

        try
        {
            var result = toolCall.Name switch
            {
                "graphrag_query" => await ExecuteGraphRagQueryAsync(toolCall.Arguments, cancellationToken),
                "entity_lookup" => await ExecuteEntityLookupAsync(toolCall.Arguments, cancellationToken),
                "graph_expansion" => await ExecuteGraphExpansionAsync(toolCall.Arguments, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown tool: {toolCall.Name}")
            };

            return new ToolCallResult
            {
                Content = new List<ToolContent>
                {
                    new ToolContent { Type = "text", Text = result }
                },
                IsError = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolCall.Name);
            return new ToolCallResult
            {
                Content = new List<ToolContent>
                {
                    new ToolContent { Type = "text", Text = $"Error: {ex.Message}" }
                },
                IsError = true
            };
        }
    }

    private async Task<string> ExecuteGraphRagQueryAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var query = GetArgument<string>(arguments, "query");
        var userId = GetArgument<string>(arguments, "user_id", "mcp-user");
        var conversationId = GetArgument<string>(arguments, "conversation_id", Guid.NewGuid().ToString());

        var response = await _orchestratorClient.AskAsync(
            query,
            userId,
            conversationId,
            cancellationToken: cancellationToken);

        // Format response with citations
        var result = $"{response.Answer}\n\n";
        if (response.Citations.Any())
        {
            result += "**Sources:**\n";
            foreach (var citation in response.Citations)
            {
                result += $"- [{citation.Title}]({citation.Url})\n";
            }
        }

        if (response.Trace != null)
        {
            result += $"\n**Trace Information:**\n";
            result += $"- Linked Entities: {string.Join(", ", response.Trace.LinkedEntities)}\n";
            result += $"- Expanded Entities: {string.Join(", ", response.Trace.ExpandedEntityIds)}\n";
        }

        return result;
    }

    private async Task<string> ExecuteEntityLookupAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var entityId = GetArgument<string>(arguments, "entity_id");
        var result = await _orchestratorClient.GetEntityInfoAsync(entityId, cancellationToken);
        return result;
    }

    private async Task<string> ExecuteGraphExpansionAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var entityId = GetArgument<string>(arguments, "entity_id");
        var hops = GetArgument<int>(arguments, "hops", 1);

        var result = await _orchestratorClient.ExpandGraphAsync(entityId, hops, cancellationToken);
        return result;
    }

    private T GetArgument<T>(Dictionary<string, object> arguments, string name, T? defaultValue = default)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            if (defaultValue != null)
                return defaultValue;
            throw new ArgumentException($"Missing required argument: {name}");
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.Deserialize<T>() ?? throw new InvalidOperationException($"Failed to deserialize {name}");
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private List<McpTool> InitializeTools()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "graphrag_query",
                Description = "Query the GraphRAG knowledge base. This tool performs entity linking, graph expansion, hybrid retrieval, and answer generation to provide comprehensive answers with citations.",
                InputSchema = new McpToolInputSchema
                {
                    Properties = new Dictionary<string, McpSchemaProperty>
                    {
                        ["query"] = new McpSchemaProperty
                        {
                            Type = "string",
                            Description = "The question or query to ask the GraphRAG system"
                        },
                        ["user_id"] = new McpSchemaProperty
                        {
                            Type = "string",
                            Description = "Optional user identifier (defaults to 'mcp-user')"
                        },
                        ["conversation_id"] = new McpSchemaProperty
                        {
                            Type = "string",
                            Description = "Optional conversation identifier for tracking context"
                        }
                    },
                    Required = new List<string> { "query" }
                }
            },
            new McpTool
            {
                Name = "entity_lookup",
                Description = "Look up information about a specific entity in the knowledge graph. Entities include services, processes, teams, databases, and other system components.",
                InputSchema = new McpToolInputSchema
                {
                    Properties = new Dictionary<string, McpSchemaProperty>
                    {
                        ["entity_id"] = new McpSchemaProperty
                        {
                            Type = "string",
                            Description = "The identifier of the entity to look up (e.g., 'service-a', 'process-x', 'team-y')"
                        }
                    },
                    Required = new List<string> { "entity_id" }
                }
            },
            new McpTool
            {
                Name = "graph_expansion",
                Description = "Expand the knowledge graph from a starting entity to discover related entities and relationships. Useful for understanding dependencies, ownership, and connections.",
                InputSchema = new McpToolInputSchema
                {
                    Properties = new Dictionary<string, McpSchemaProperty>
                    {
                        ["entity_id"] = new McpSchemaProperty
                        {
                            Type = "string",
                            Description = "The starting entity identifier to expand from"
                        },
                        ["hops"] = new McpSchemaProperty
                        {
                            Type = "integer",
                            Description = "Number of relationship hops to expand (1-3, defaults to 1)"
                        }
                    },
                    Required = new List<string> { "entity_id" }
                }
            }
        };
    }
}
