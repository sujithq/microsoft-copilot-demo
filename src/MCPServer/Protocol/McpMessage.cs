using System.Text.Json.Serialization;

namespace MCPServer.Protocol;

/// <summary>
/// Base class for MCP JSON-RPC messages
/// </summary>
public record McpMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
}

/// <summary>
/// MCP Request message
/// </summary>
public record McpRequest : McpMessage
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

/// <summary>
/// MCP Response message
/// </summary>
public record McpResponse : McpMessage
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public McpError? Error { get; init; }
}

/// <summary>
/// MCP Error object
/// </summary>
public record McpError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>
/// MCP Notification message (no response expected)
/// </summary>
public record McpNotification : McpMessage
{
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public object? Params { get; init; }
}
