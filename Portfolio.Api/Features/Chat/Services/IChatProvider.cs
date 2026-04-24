using Portfolio.Api.Features.Chat.Contracts;

namespace Portfolio.Api.Features.Chat.Services;

public interface IChatProvider
{
    string Name { get; }
    Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken);
}
