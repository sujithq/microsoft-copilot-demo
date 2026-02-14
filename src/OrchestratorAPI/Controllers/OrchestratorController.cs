using Microsoft.AspNetCore.Mvc;
using OrchestratorAPI.Models;
using OrchestratorAPI.Services;

namespace OrchestratorAPI.Controllers;

[ApiController]
[Route("api")]
public class OrchestratorController : ControllerBase
{
    private readonly IOrchestratorService _orchestratorService;
    private readonly ILogger<OrchestratorController> _logger;

    public OrchestratorController(
        IOrchestratorService orchestratorService,
        ILogger<OrchestratorController> logger)
    {
        _orchestratorService = orchestratorService;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AskResponse>> Ask(
        [FromBody] AskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received ask request for query: {Query}", request.Query);

            var response = await _orchestratorService.ProcessQueryAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ask request");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
