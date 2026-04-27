namespace Portfolio.Api.Features.Chat.Contracts;

public sealed record ChatRequest(string Provider, string Message, string? Model, bool? UseRag = true);
