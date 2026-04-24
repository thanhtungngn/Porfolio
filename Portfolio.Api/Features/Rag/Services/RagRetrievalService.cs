namespace Portfolio.Api.Features.Rag.Services;

public class RagRetrievalService(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<RagRetrievalService> logger)
{
    public async Task<IReadOnlyList<RagMatch>> GetMatchesAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "RAG retrieval started. QueryLength={QueryLength}, TopK={TopK}",
            query?.Length ?? 0,
            topK);

        var queryVector = await embeddingService.GetEmbeddingAsync(query, cancellationToken);
        var results = await vectorStore.SearchAsync(queryVector, topK, cancellationToken);

        var matches = results
            .Select(r =>
            {
                var text = r.Payload.TryGetValue("text", out var textVal) ? textVal.StringValue : string.Empty;
                var source = r.Payload.TryGetValue("source", out var sourceVal) ? sourceVal.StringValue : string.Empty;
                return new RagMatch(text, source, r.Score);
            })
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .ToList();

        logger.LogInformation(
            "RAG retrieval completed. MatchCount={MatchCount}",
            matches.Count);

        return matches;
    }

    public async Task<string> GetContextAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var matches = await GetMatchesAsync(query, topK, cancellationToken);
        if (matches.Count == 0)
            return string.Empty;

        return string.Join("\n\n", matches.Select(m => m.Text));
    }
}

public sealed record RagMatch(string Text, string Source, float Score);
