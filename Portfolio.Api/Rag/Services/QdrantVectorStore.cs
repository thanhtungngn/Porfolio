using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public interface IVectorStore
{
    Task EnsureCollectionAsync(uint vectorSize, CancellationToken cancellationToken = default);
    Task UpsertAsync(IEnumerable<Qdrant.Client.Grpc.PointStruct> points, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Qdrant.Client.Grpc.ScoredPoint>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
}

public class QdrantVectorStore(IOptions<QdrantOptions> options) : IVectorStore
{
    private readonly QdrantClient _client = CreateClient(options.Value);
    private readonly QdrantOptions _settings = options.Value;
    private bool _collectionEnsured;

    private static QdrantClient CreateClient(QdrantOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            return new QdrantClient(opts.Host, opts.Port, apiKey: opts.ApiKey, https: opts.Https);

        return new QdrantClient(opts.Host, opts.Port);
    }

    public async Task EnsureCollectionAsync(uint vectorSize, CancellationToken cancellationToken = default)
    {
        if (_collectionEnsured)
            return;

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

        _collectionEnsured = true;
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
