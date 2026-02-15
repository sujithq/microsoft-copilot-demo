using Azure.AI.OpenAI;
using OrchestratorAPI.Models;
using OpenAI.Chat;

namespace OrchestratorAPI.Services;

/// <summary>
/// Service for generating answers using Microsoft Foundry (gpt-5.2).
/// Microsoft Foundry is a unified AI platform that includes Azure OpenAI capabilities.
/// This service connects to an Azure AI Services (AIServices kind) account which provides
/// access to multiple AI models including GPT-4, GPT-5.2, and embeddings.
/// </summary>
public class AnswerGenerationService : IAnswerGenerationService
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly ILogger<AnswerGenerationService> _logger;

    public AnswerGenerationService(
        AzureOpenAIClient openAIClient,
        IConfiguration configuration,
        ILogger<AnswerGenerationService> logger)
    {
        _openAIClient = openAIClient;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-5.2";
        _logger = logger;
    }

    public async Task<AnswerResult> GenerateAnswerAsync(
        string query, 
        List<SearchResult> chunks, 
        GraphContext? graphContext = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build context from chunks
            var context = string.Join("\n\n", chunks.Select((chunk, idx) => 
                $"[{idx + 1}] {chunk.Title}\n{chunk.Content}\nSource: {chunk.Url}"));

            // Build graph context if available
            var graphInfo = graphContext != null
                ? $"\n\nRelated Entities: {string.Join(", ", graphContext.EntityList)}\n" +
                  $"Relationships: {string.Join(", ", graphContext.RelationshipList)}"
                : "";

            // Build system prompt
            var systemPrompt = @"You are an expert assistant that answers questions based on the provided context.
Use the context below to answer the user's question accurately and concisely.
Always cite your sources using [1], [2], etc. format matching the context numbering.
If the context doesn't contain enough information to answer the question, say so.";

            // Build user prompt
            var userPrompt = $@"Context:
{context}{graphInfo}

Question: {query}

Please provide a comprehensive answer with citations.";

            var chatClient = _openAIClient.GetChatClient(_deploymentName);

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken);

            var answer = response.Value.Content[0].Text;

            // Extract citations from the chunks
            var citations = chunks.Select(chunk => new Citation
            {
                Title = chunk.Title,
                Url = chunk.Url,
                ChunkId = chunk.ChunkId
            }).ToList();

            _logger.LogInformation("Generated answer with {CitationCount} citations", citations.Count);

            return new AnswerResult
            {
                Answer = answer,
                Citations = citations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating answer");
            return new AnswerResult
            {
                Answer = "I apologize, but I encountered an error while generating the answer.",
                Citations = new List<Citation>()
            };
        }
    }
}
