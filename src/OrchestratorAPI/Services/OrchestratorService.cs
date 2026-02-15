using OrchestratorAPI.Models;

namespace OrchestratorAPI.Services;

public interface IOrchestratorService
{
    Task<AskResponse> ProcessQueryAsync(AskRequest request, CancellationToken cancellationToken = default);
}

public class OrchestratorService : IOrchestratorService
{
    private readonly IEntityLinkingService _entityLinkingService;
    private readonly IGraphExpansionService _graphExpansionService;
    private readonly IHybridRetrievalService _hybridRetrievalService;
    private readonly IAnswerGenerationService _answerGenerationService;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        IEntityLinkingService entityLinkingService,
        IGraphExpansionService graphExpansionService,
        IHybridRetrievalService hybridRetrievalService,
        IAnswerGenerationService answerGenerationService,
        ILogger<OrchestratorService> logger)
    {
        _entityLinkingService = entityLinkingService;
        _graphExpansionService = graphExpansionService;
        _hybridRetrievalService = hybridRetrievalService;
        _answerGenerationService = answerGenerationService;
        _logger = logger;
    }

    public async Task<AskResponse> ProcessQueryAsync(
        AskRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing query for user {UserId} in conversation {ConversationId}",
            request.User.AadObjectId, request.ConversationId);

        // Step 1: Entity Linking
        var linkedEntityIds = await _entityLinkingService.LinkEntitiesAsync(
            request.Query, 
            cancellationToken);

        // Step 2: Graph Expansion
        var expansionResult = await _graphExpansionService.ExpandGraphAsync(
            linkedEntityIds, 
            maxHops: 2, 
            cancellationToken);

        // Step 3: Hybrid Retrieval
        var searchResults = await _hybridRetrievalService.RetrieveAsync(
            request.Query, 
            expansionResult.ExpandedEntityIds, 
            topK: 5, 
            cancellationToken);

        // Step 4: Answer Generation
        var answerResult = await _answerGenerationService.GenerateAnswerAsync(
            request.Query, 
            searchResults, 
            cancellationToken: cancellationToken);

        // Build search filter for trace
        var searchFilter = expansionResult.ExpandedEntityIds.Any()
            ? $"entityIds/any(e: {string.Join(" or ", expansionResult.ExpandedEntityIds.Select(id => $"e eq '{id}'"))})"
            : "";

        // Build response
        var response = new AskResponse
        {
            Answer = answerResult.Answer,
            Citations = answerResult.Citations,
            Trace = new TraceInfo
            {
                LinkedEntities = linkedEntityIds,
                ExpandedEntityIds = expansionResult.ExpandedEntityIds,
                SearchFilter = searchFilter
            }
        };

        _logger.LogInformation(
            "Query processed successfully with {CitationCount} citations",
            response.Citations.Count);

        return response;
    }
}
