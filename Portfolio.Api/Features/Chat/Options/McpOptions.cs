namespace Portfolio.Api.Features.Chat.Options;

public sealed class McpOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string ToolsPath { get; init; } = "/tools";
    public string InvokePathTemplate { get; init; } = "/tools/{toolName}";
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
}
