namespace Portfolio.Api.Features.Chat.Contracts;

public sealed record ChatResponse(string Reply, IReadOnlyList<RagSource>? Sources = null);
