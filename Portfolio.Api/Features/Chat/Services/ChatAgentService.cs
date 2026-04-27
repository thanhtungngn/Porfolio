using Portfolio.Api.Features.Chat.Contracts;
using Portfolio.Api.Features.Chat.Exceptions;
using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Api.Features.Chat.Services;

internal sealed class ChatAgentService(
    IEnumerable<IChatProvider> providers,
    RagRetrievalService ragRetrievalService)
{
    private readonly IReadOnlyDictionary<string, IChatProvider> _providers = providers
        .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    public async Task<ChatResponse> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var augmentedRequest = request;
        IReadOnlyList<RagSource> sources = [];

        if (request.UseRag == true && !string.IsNullOrWhiteSpace(request.Message))
        {
            var matches = await ragRetrievalService.GetMatchesAsync(request.Message, cancellationToken: cancellationToken);
            if (matches.Count > 0)
            {
                var context = string.Join("\n\n", matches.Select(m => m.Text));
                var augmentedMessage = $"Use the following context to answer the question.\n\nContext:\n{context}\n\nQuestion:\n{request.Message}";
                augmentedRequest = request with { Message = augmentedMessage };

                sources = matches
                    .Where(m => !string.IsNullOrWhiteSpace(m.Source))
                    .Select(m => new RagSource(m.Source, m.Score))
                    .GroupBy(s => s.Source, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.Score).First())
                    .OrderByDescending(s => s.Score)
                    .ToList();
            }
        }

        if (!_providers.TryGetValue(augmentedRequest.Provider?.Trim() ?? string.Empty, out var provider))
        {
            throw new ChatValidationException("Provider must be either 'openai' or 'ollama'.");
        }

        var reply = await provider.GetReplyAsync(augmentedRequest, cancellationToken);
        return new ChatResponse(reply, sources);
    }
}
