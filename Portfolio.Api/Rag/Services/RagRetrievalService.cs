public class RagRetrievalService(IEmbeddingService embeddingService, IVectorStore vectorStore)
{
    public async Task<string> GetContextAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryVector = await embeddingService.GetEmbeddingAsync(query, cancellationToken);
        var results = await vectorStore.SearchAsync(queryVector, topK, cancellationToken);

        if (results.Count == 0)
            return string.Empty;

        var chunks = results.Select(r =>
            r.Payload.TryGetValue("text", out var val) ? val.StringValue : string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return string.Join("\n\n", chunks);
    }
}
