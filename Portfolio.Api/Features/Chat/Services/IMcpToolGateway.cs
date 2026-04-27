namespace Portfolio.Api.Features.Chat.Services;

internal interface IMcpToolGateway
{
    Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(CancellationToken cancellationToken);
    Task<string> InvokeToolAsync(string toolName, string? argumentsJson, CancellationToken cancellationToken);
}

internal sealed record McpToolDefinition(string Name, string Description, string InputSchemaJson);
