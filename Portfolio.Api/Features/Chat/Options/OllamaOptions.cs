namespace Portfolio.Api.Features.Chat.Options;

public sealed class OllamaOptions
{
    public string Endpoint { get; init; } = "http://localhost:11434/api/chat";
    public string DefaultModel { get; init; } = "llama3.2";
    public string SystemPrompt { get; init; } = "You are an assistant for a portfolio website. Be concise and practical.";
}
