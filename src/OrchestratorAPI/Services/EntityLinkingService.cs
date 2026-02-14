using Microsoft.Azure.Cosmos;
using OrchestratorAPI.Models;

namespace OrchestratorAPI.Services;

public class EntityLinkingService : IEntityLinkingService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseId;
    private readonly string _containerId;
    private readonly ILogger<EntityLinkingService> _logger;

    public EntityLinkingService(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<EntityLinkingService> logger)
    {
        _cosmosClient = cosmosClient;
        _databaseId = configuration["CosmosDb:DatabaseId"] ?? "graphrag";
        _containerId = configuration["CosmosDb:EntitiesContainerId"] ?? "entities";
        _logger = logger;
    }

    public async Task<List<string>> LinkEntitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = _cosmosClient.GetContainer(_databaseId, _containerId);
            
            // Simple entity linking: extract potential entity names from query
            // In production, use NER or LLM-based entity extraction
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var linkedEntityIds = new List<string>();

            // Query Cosmos to find matching entities by name or aliases
            var queryText = "SELECT c.id FROM c WHERE CONTAINS(LOWER(@query), LOWER(c.name))";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@query", query);

            var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    linkedEntityIds.Add((string)item.id);
                }
            }

            _logger.LogInformation("Linked {Count} entities from query", linkedEntityIds.Count);
            return linkedEntityIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking entities");
            return new List<string>();
        }
    }
}
