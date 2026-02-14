using System.Text;
using System.Text.Json;
using MCPServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MCPServer.Services;

/// <summary>
/// Service for communicating with the Orchestrator API
/// </summary>
public class OrchestratorClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestratorClient> _logger;
    private readonly string _baseUrl;

    public OrchestratorClient(HttpClient httpClient, IConfiguration configuration, ILogger<OrchestratorClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["OrchestratorApi:BaseUrl"] ?? "http://localhost:5000";
    }

    /// <summary>
    /// Ask a question to the GraphRAG orchestrator
    /// </summary>
    public async Task<OrchestratorResponse> AskAsync(
        string query,
        string userId = "mcp-user",
        string conversationId = "mcp-session",
        string tenantId = "default",
        string locale = "en-US",
        CancellationToken cancellationToken = default)
    {
        var request = new OrchestratorRequest
        {
            User = new UserInfo { AadObjectId = userId },
            ConversationId = conversationId,
            Query = query,
            Context = new ContextInfo { TenantId = tenantId, Locale = locale }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending query to Orchestrator: {Query}", query);

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/ask", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OrchestratorResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Orchestrator response");
        }

        _logger.LogInformation("Received response from Orchestrator with {CitationCount} citations", result.Citations.Count);

        return result;
    }

    /// <summary>
    /// Get entity information (simulated for now)
    /// </summary>
    public async Task<string> GetEntityInfoAsync(string entityId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting entity info for: {EntityId}", entityId);
        
        // For now, use the ask endpoint to get info about the entity
        var response = await AskAsync($"What is {entityId}?", cancellationToken: cancellationToken);
        return response.Answer;
    }

    /// <summary>
    /// Expand graph from an entity (simulated for now)
    /// </summary>
    public async Task<string> ExpandGraphAsync(string entityId, int hops = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Expanding graph from entity: {EntityId}, hops: {Hops}", entityId, hops);
        
        // Use the ask endpoint to get related entities
        var response = await AskAsync(
            $"What entities are related to {entityId}? Include dependencies and relationships.",
            cancellationToken: cancellationToken);
        
        return response.Answer;
    }
}
