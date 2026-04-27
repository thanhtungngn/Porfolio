using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Rag.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Portfolio.Api.Features.Rag.Services;

public interface IVectorStore
{
    Task EnsureCollectionAsync(uint vectorSize, CancellationToken cancellationToken = default);
    Task UpsertAsync(IEnumerable<Qdrant.Client.Grpc.PointStruct> points, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Qdrant.Client.Grpc.ScoredPoint>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
}

public class QdrantVectorStore(
    IOptions<QdrantOptions> options,
    ILogger<QdrantVectorStore> logger) : IVectorStore
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

        logger.LogInformation(
            "Ensuring Qdrant collection. Collection={Collection}, VectorSize={VectorSize}, Host={Host}, Port={Port}",
            _settings.CollectionName,
            vectorSize,
            _settings.Host,
            _settings.Port);

        var exists = await _client.CollectionExistsAsync(_settings.CollectionName, cancellationToken);
        if (!exists)
        {
            logger.LogInformation(
                "Creating Qdrant collection. Collection={Collection}, VectorSize={VectorSize}",
                _settings.CollectionName,
                vectorSize);

            await _client.CreateCollectionAsync(
                _settings.CollectionName,
                new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: cancellationToken);
        }

        logger.LogInformation(
            "Qdrant collection is ready. Collection={Collection}",
            _settings.CollectionName);

        _collectionEnsured = true;
    }

    public async Task UpsertAsync(IEnumerable<PointStruct> points, CancellationToken cancellationToken = default)
    {
        var pointList = points.ToList();
        logger.LogInformation(
            "Upserting vectors. Collection={Collection}, Count={Count}",
            _settings.CollectionName,
            pointList.Count);

        await _client.UpsertAsync(_settings.CollectionName, pointList, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Searching vectors. Collection={Collection}, QueryDimensions={Dimensions}, TopK={TopK}",
            _settings.CollectionName,
            queryVector.Length,
            topK);

        var results = await _client.SearchAsync(
            _settings.CollectionName,
            queryVector,
            limit: (ulong)topK,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Vector search completed. Collection={Collection}, ResultCount={ResultCount}",
            _settings.CollectionName,
            results.Count);

        return results;
    }
}
