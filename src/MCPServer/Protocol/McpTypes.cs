using System.Text.Json.Serialization;

namespace MCPServer.Protocol;

/// <summary>
/// Server capabilities
/// </summary>
public record ServerCapabilities
{
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; init; }

    [JsonPropertyName("resources")]
    public ResourcesCapability? Resources { get; init; }
}

public record ToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

public record ResourcesCapability
{
    [JsonPropertyName("subscribe")]
    public bool? Subscribe { get; init; }

    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

/// <summary>
/// Server information
/// </summary>
public record ServerInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

/// <summary>
/// Initialize result
/// </summary>
public record InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; init; }

    [JsonPropertyName("capabilities")]
    public required ServerCapabilities Capabilities { get; init; }

    [JsonPropertyName("serverInfo")]
    public required ServerInfo ServerInfo { get; init; }
}

/// <summary>
/// List tools result
/// </summary>
public record ListToolsResult
{
    [JsonPropertyName("tools")]
    public required List<McpTool> Tools { get; init; }
}
