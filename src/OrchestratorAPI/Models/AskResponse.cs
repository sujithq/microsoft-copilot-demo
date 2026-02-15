namespace OrchestratorAPI.Models;

public record AskResponse
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
