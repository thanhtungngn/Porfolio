using NSubstitute;
using Qdrant.Client.Grpc;

namespace Portfolio.Tests;

public class IngestionServiceTests
{
    private readonly PdfDocumentLoader _pdfLoader       = Substitute.For<PdfDocumentLoader>();
    private readonly DocumentChunker   _chunker         = new();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore      _vectorStore      = Substitute.For<IVectorStore>();

    private IngestionService CreateSut() =>
        new(_pdfLoader, _chunker, _embeddingService, _vectorStore);

    [Fact]
    public async Task IngestTextAsync_ReturnsCorrectChunkCount()
    {
        // 600 words → 2 chunks (chunkSize=500, overlap=50)
        var text = string.Join(' ', Enumerable.Repeat("word", 600));
        var fakeEmbedding = new float[1536];

        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fakeEmbedding);

        var sut = CreateSut();
        var count = await sut.IngestTextAsync(text, "test-source");

        Assert.True(count >= 2);
    }

    [Fact]
    public async Task IngestTextAsync_CallsEnsureCollectionOnce()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 10));
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);

        var sut = CreateSut();
        await sut.IngestTextAsync(text, "source");

        await _vectorStore.Received(1).EnsureCollectionAsync(1536u, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestTextAsync_CallsUpsertOnce()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 10));
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);

        var sut = CreateSut();
        await sut.IngestTextAsync(text, "source");

        await _vectorStore.Received(1).UpsertAsync(Arg.Any<IEnumerable<PointStruct>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestTextAsync_EmptyText_ReturnsZeroChunks()
    {
        var sut = CreateSut();
        var count = await sut.IngestTextAsync("", "source");

        Assert.Equal(0, count);
        await _embeddingService.DidNotReceive().GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
