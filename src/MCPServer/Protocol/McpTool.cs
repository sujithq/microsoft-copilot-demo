using System.Text.Json.Serialization;

namespace MCPServer.Protocol;

/// <summary>
/// MCP Tool definition
/// </summary>
public record McpTool
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("inputSchema")]
    public required McpToolInputSchema InputSchema { get; init; }
}

/// <summary>
/// Tool input schema (JSON Schema format)
/// </summary>
public record McpToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "object";

    [JsonPropertyName("properties")]
    public required Dictionary<string, McpSchemaProperty> Properties { get; init; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; init; }
}

/// <summary>
/// Schema property definition
/// </summary>
public record McpSchemaProperty
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("enum")]
    public List<string>? Enum { get; init; }
}

/// <summary>
/// Tool call parameters
/// </summary>
public record ToolCallParams
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("arguments")]
    public required Dictionary<string, object> Arguments { get; init; }
}

/// <summary>
/// Tool call result
/// </summary>
public record ToolCallResult
{
    [JsonPropertyName("content")]
    public required List<ToolContent> Content { get; init; }

    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}

/// <summary>
/// Tool content (text or other)
/// </summary>
public record ToolContent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
