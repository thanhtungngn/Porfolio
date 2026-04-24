using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Api.Features.Rag.Contracts;

public sealed record RagSearchResponse(string Context, IReadOnlyList<RagMatch> Matches);
