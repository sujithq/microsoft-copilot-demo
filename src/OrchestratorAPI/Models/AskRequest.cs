namespace OrchestratorAPI.Models;

public record AskRequest
{
    public required UserInfo User { get; init; }
    public required string ConversationId { get; init; }
    public required string Query { get; init; }
    public required ContextInfo Context { get; init; }
}

public record UserInfo
{
    public required string AadObjectId { get; init; }
}

public record ContextInfo
{
    public required string TenantId { get; init; }
    public required string Locale { get; init; }
}
