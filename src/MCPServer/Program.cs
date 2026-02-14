using System.Text.Json;
using MCPServer.Protocol;
using MCPServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCPServer;

class Program
{
    static async Task Main(string[] args)
    {
        // Build the host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add HTTP client for Orchestrator API
                services.AddHttpClient<OrchestratorClient>();

                // Add MCP services
                services.AddSingleton<McpToolService>();
                services.AddSingleton<McpServerHandler>();

                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                    builder.AddConsole();
                });
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var handler = host.Services.GetRequiredService<McpServerHandler>();

        logger.LogInformation("GraphRAG MCP Server starting...");
        logger.LogInformation("Reading JSON-RPC messages from stdin, writing to stdout");

        // Process messages from stdin
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        try
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    // Parse the JSON-RPC request
                    var request = JsonSerializer.Deserialize<McpRequest>(line, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (request == null)
                    {
                        logger.LogWarning("Failed to deserialize request");
                        continue;
                    }

                    // Handle the request
                    var response = await handler.HandleRequestAsync(request, CancellationToken.None);

                    // Write the response
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });

                    await writer.WriteLineAsync(responseJson);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "JSON parsing error");
                    
                    // Send error response
                    var errorResponse = new McpResponse
                    {
                        Id = "unknown",
                        Error = new McpError
                        {
                            Code = -32700,
                            Message = "Parse error"
                        }
                    };

                    var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await writer.WriteLineAsync(errorJson);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error processing request");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in MCP server");
            return;
        }

        logger.LogInformation("GraphRAG MCP Server shutting down");
    }
}
