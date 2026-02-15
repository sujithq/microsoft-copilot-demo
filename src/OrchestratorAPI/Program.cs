using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Azure.Cosmos;
using OrchestratorAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Azure Cosmos DB
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["CosmosDb:Endpoint"] ?? throw new InvalidOperationException("CosmosDb:Endpoint is required");
    var credential = new DefaultAzureCredential();
    return new CosmosClient(endpoint, credential);
});

// Configure Azure AI Search
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureSearch:Endpoint"] ?? throw new InvalidOperationException("AzureSearch:Endpoint is required");
    var indexName = config["AzureSearch:IndexName"] ?? "chunks";
    var credential = new DefaultAzureCredential();
    return new SearchClient(new Uri(endpoint), indexName, credential);
});

// Configure Microsoft Foundry (Azure AI Services with OpenAI)
// Microsoft Foundry provides unified access to Azure OpenAI models (gpt-5.2, embeddings, etc.)
// through Azure AI Services accounts
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required");
    var credential = new DefaultAzureCredential();
    return new AzureOpenAIClient(new Uri(endpoint), credential);
});

// Register GraphRAG services
builder.Services.AddScoped<IEntityLinkingService, EntityLinkingService>();
builder.Services.AddScoped<IGraphExpansionService, GraphExpansionService>();
builder.Services.AddScoped<IHybridRetrievalService, HybridRetrievalService>();
builder.Services.AddScoped<IAnswerGenerationService, AnswerGenerationService>();
builder.Services.AddScoped<IOrchestratorService, OrchestratorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
