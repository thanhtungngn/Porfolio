using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class QdrantVectorStore(IOptions<QdrantOptions> options)
{
    private readonly QdrantClient _client = CreateClient(options.Value);
    private readonly QdrantOptions _settings = options.Value;

    private static QdrantClient CreateClient(QdrantOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            return new QdrantClient(opts.Host, opts.Port, apiKey: opts.ApiKey);

        return new QdrantClient(opts.Host, opts.Port);
    }

    public async Task EnsureCollectionAsync(uint vectorSize, CancellationToken cancellationToken = default)
    {
        var exists = await _client.CollectionExistsAsync(_settings.CollectionName, cancellationToken);
        if (!exists)
        {
            await _client.CreateCollectionAsync(
                _settings.CollectionName,
                new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: cancellationToken);
        }
    }

    public async Task UpsertAsync(IEnumerable<PointStruct> points, CancellationToken cancellationToken = default)
    {
        await _client.UpsertAsync(_settings.CollectionName, points.ToList(), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default)
    {
        return await _client.SearchAsync(
            _settings.CollectionName,
            queryVector,
            limit: (ulong)topK,
            cancellationToken: cancellationToken);
    }
}
