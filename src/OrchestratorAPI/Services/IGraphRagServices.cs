using OrchestratorAPI.Models;

namespace OrchestratorAPI.Services;

public interface IEntityLinkingService
{
    Task<List<string>> LinkEntitiesAsync(string query, CancellationToken cancellationToken = default);
}

public interface IGraphExpansionService
{
    Task<GraphExpansionResult> ExpandGraphAsync(List<string> entityIds, int maxHops = 2, CancellationToken cancellationToken = default);
}

public record GraphExpansionResult
{
    public required List<string> ExpandedEntityIds { get; init; }
    public required List<string> EvidenceChunkIds { get; init; }
}

public interface IHybridRetrievalService
{
    Task<List<SearchResult>> RetrieveAsync(string query, List<string> entityIds, int topK = 5, CancellationToken cancellationToken = default);
}

public record SearchResult
{
    public required string ChunkId { get; init; }
    public required string Content { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public double Score { get; init; }
}

public interface IAnswerGenerationService
{
    Task<AnswerResult> GenerateAnswerAsync(string query, List<SearchResult> chunks, GraphContext? graphContext = null, CancellationToken cancellationToken = default);
}

public record AnswerResult
{
    public required string Answer { get; init; }
    public required List<Citation> Citations { get; init; }
}

public record GraphContext
{
    public required List<string> EntityList { get; init; }
    public required List<string> RelationshipList { get; init; }
}
