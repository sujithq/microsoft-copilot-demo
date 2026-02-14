namespace MCPServer.Models;

/// <summary>
/// Request to Orchestrator API
/// </summary>
public record OrchestratorRequest
{
    public required UserInfo User { get; init; }
    public required string ConversationId { get; init; }
    public required string Query { get; init; }
    public required ContextInfo Context { get; init; }
}

public record UserInfo
{
    public required string AadObjectId { get; init; }
}

public record ContextInfo
{
    public required string TenantId { get; init; }
    public required string Locale { get; init; }
}

/// <summary>
/// Response from Orchestrator API
/// </summary>
public record OrchestratorResponse
{
    public required string Answer { get; init; }
    public required List<Citation> Citations { get; init; }
    public TraceInfo? Trace { get; init; }
}

public record Citation
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string ChunkId { get; init; }
}

public record TraceInfo
{
    public required List<string> LinkedEntities { get; init; }
    public required List<string> ExpandedEntityIds { get; init; }
    public required string SearchFilter { get; init; }
}
