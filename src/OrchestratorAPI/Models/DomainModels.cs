namespace OrchestratorAPI.Models;

public record Entity
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public List<string> Aliases { get; init; } = new();
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public record Relation
{
    public required string Id { get; init; }
    public required string SourceEntityId { get; init; }
    public required string TargetEntityId { get; init; }
    public required string RelationType { get; init; }
    public List<string> EvidenceChunkIds { get; init; } = new();
}

public record Chunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public List<string> EntityIds { get; init; } = new();
    public List<float> ContentVector { get; init; } = new();
}
