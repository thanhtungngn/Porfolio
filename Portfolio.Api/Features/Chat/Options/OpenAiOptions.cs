namespace Portfolio.Api.Features.Chat.Options;

public sealed class OpenAiOptions
{
    public string Endpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
    public string DefaultModel { get; init; } = "gpt-4o-mini";
    public string SystemPrompt { get; init; } = "You are an assistant for a portfolio website. Be concise and practical.";
    public string ApiKey { get; init; } = string.Empty;
}
