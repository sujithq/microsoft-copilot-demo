using Microsoft.Azure.Cosmos;

namespace OrchestratorAPI.Services;

public class GraphExpansionService : IGraphExpansionService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseId;
    private readonly string _relationsContainerId;
    private readonly ILogger<GraphExpansionService> _logger;

    public GraphExpansionService(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<GraphExpansionService> logger)
    {
        _cosmosClient = cosmosClient;
        _databaseId = configuration["CosmosDb:DatabaseId"] ?? "graphrag";
        _relationsContainerId = configuration["CosmosDb:RelationsContainerId"] ?? "relations";
        _logger = logger;
    }

    public async Task<GraphExpansionResult> ExpandGraphAsync(
        List<string> entityIds, 
        int maxHops = 2, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var container = _cosmosClient.GetContainer(_databaseId, _relationsContainerId);
            var expandedEntityIds = new HashSet<string>(entityIds);
            var evidenceChunkIds = new HashSet<string>();
            var currentLevel = new HashSet<string>(entityIds);

            for (int hop = 0; hop < maxHops && currentLevel.Any(); hop++)
            {
                var nextLevel = new HashSet<string>();

                foreach (var entityId in currentLevel)
                {
                    // Query relations where this entity is source or target
                    var queryText = @"
                        SELECT c.sourceEntityId, c.targetEntityId, c.evidenceChunkIds 
                        FROM c 
                        WHERE c.sourceEntityId = @entityId OR c.targetEntityId = @entityId";
                    
                    var queryDefinition = new QueryDefinition(queryText)
                        .WithParameter("@entityId", entityId);

                    var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync(cancellationToken);
                        foreach (var item in response)
                        {
                            string source = item.sourceEntityId;
                            string target = item.targetEntityId;

                            // Add neighboring entities
                            if (!expandedEntityIds.Contains(source))
                            {
                                nextLevel.Add(source);
                                expandedEntityIds.Add(source);
                            }
                            if (!expandedEntityIds.Contains(target))
                            {
                                nextLevel.Add(target);
                                expandedEntityIds.Add(target);
                            }

                            // Collect evidence chunk IDs
                            if (item.evidenceChunkIds != null)
                            {
                                foreach (var chunkId in item.evidenceChunkIds)
                                {
                                    evidenceChunkIds.Add((string)chunkId);
                                }
                            }
                        }
                    }
                }

                currentLevel = nextLevel;
            }

            _logger.LogInformation(
                "Expanded {InitialCount} entities to {ExpandedCount} entities with {ChunkCount} evidence chunks",
                entityIds.Count, expandedEntityIds.Count, evidenceChunkIds.Count);

            return new GraphExpansionResult
            {
                ExpandedEntityIds = expandedEntityIds.ToList(),
                EvidenceChunkIds = evidenceChunkIds.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding graph");
            return new GraphExpansionResult
            {
                ExpandedEntityIds = entityIds,
                EvidenceChunkIds = new List<string>()
            };
        }
    }
}
