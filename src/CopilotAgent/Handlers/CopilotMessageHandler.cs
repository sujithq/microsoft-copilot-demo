using System.Text;
using System.Text.Json;
using Azure.Identity;
using CopilotAgent.Models;
using Microsoft.Bot.Schema;

namespace CopilotAgent.Handlers;

public class CopilotMessageHandler
{
    private readonly HttpClient _httpClient;
    private readonly string _orchestratorBaseUrl;

    public CopilotMessageHandler(HttpClient httpClient, string orchestratorBaseUrl)
    {
        _httpClient = httpClient;
        _orchestratorBaseUrl = orchestratorBaseUrl;
    }

    public async Task<string> HandleMessageAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        // Extract user information
        var userId = activity.From?.AadObjectId ?? activity.From?.Id ?? "unknown";
        var conversationId = activity.Conversation?.Id ?? "unknown";
        var query = activity.Text ?? "";
        var tenantId = activity.ChannelData?.ToString() ?? "unknown";

        // Build request to orchestrator
        var request = new OrchestratorRequest
        {
            User = new UserInfo { AadObjectId = userId },
            ConversationId = conversationId,
            Query = query,
            Context = new ContextInfo 
            { 
                TenantId = tenantId, 
                Locale = activity.Locale ?? "en-US" 
            }
        };

        // Call orchestrator API
        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_orchestratorBaseUrl}/api/ask",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var orchestratorResponse = JsonSerializer.Deserialize<OrchestratorResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (orchestratorResponse == null)
        {
            return "I apologize, but I couldn't process your request.";
        }

        // Format response with citations for Copilot
        return FormatResponseWithCitations(orchestratorResponse);
    }

    private string FormatResponseWithCitations(OrchestratorResponse response)
    {
        var sb = new StringBuilder();
        sb.AppendLine(response.Answer);

        if (response.Citations.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Sources:**");
            
            for (int i = 0; i < response.Citations.Count; i++)
            {
                var citation = response.Citations[i];
                sb.AppendLine($"{i + 1}. [{citation.Title}]({citation.Url})");
            }
        }

        return sb.ToString();
    }
}
