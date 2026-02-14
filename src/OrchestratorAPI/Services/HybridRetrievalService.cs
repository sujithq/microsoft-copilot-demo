using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace OrchestratorAPI.Services;

public class HybridRetrievalService : IHybridRetrievalService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<HybridRetrievalService> _logger;

    public HybridRetrievalService(
        SearchClient searchClient,
        ILogger<HybridRetrievalService> logger)
    {
        _searchClient = searchClient;
        _logger = logger;
    }

    public async Task<List<SearchResult>> RetrieveAsync(
        string query, 
        List<string> entityIds, 
        int topK = 5, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build OData filter for entity IDs
            var filter = entityIds.Any() 
                ? $"entityIds/any(e: {string.Join(" or ", entityIds.Select(id => $"e eq '{id}'"))})"
                : null;

            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = topK,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Full
            };

            // Add fields to retrieve
            searchOptions.Select.Add("id");
            searchOptions.Select.Add("content");
            searchOptions.Select.Add("title");
            searchOptions.Select.Add("url");

            // Perform hybrid search (BM25 + vector)
            var searchResults = await _searchClient.SearchAsync<SearchDocument>(
                query, 
                searchOptions, 
                cancellationToken);

            var results = new List<SearchResult>();

            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                results.Add(new SearchResult
                {
                    ChunkId = result.Document["id"]?.ToString() ?? "",
                    Content = result.Document["content"]?.ToString() ?? "",
                    Title = result.Document["title"]?.ToString() ?? "",
                    Url = result.Document["url"]?.ToString() ?? "",
                    Score = result.Score ?? 0.0
                });
            }

            _logger.LogInformation(
                "Retrieved {Count} chunks for query with {EntityCount} entity filters",
                results.Count, entityIds.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chunks");
            return new List<SearchResult>();
        }
    }
}
