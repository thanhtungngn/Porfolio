using NSubstitute;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Logging;
using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Tests;

public class RagRetrievalServiceTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly ILogger<RagRetrievalService> _logger = Substitute.For<ILogger<RagRetrievalService>>();

    private RagRetrievalService CreateSut() => new(_embeddingService, _vectorStore, _logger);

    [Fact]
    public async Task GetContextAsync_ReturnsJoinedChunkTexts()
    {
        var queryVector = new float[1536];
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);

        var scored1 = new ScoredPoint { Payload = { ["text"] = new Value { StringValue = "Chunk one." } } };
        var scored2 = new ScoredPoint { Payload = { ["text"] = new Value { StringValue = "Chunk two." } } };

        _vectorStore.SearchAsync(queryVector, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoredPoint> { scored1, scored2 });

        var sut = CreateSut();
        var context = await sut.GetContextAsync("some query");

        Assert.Contains("Chunk one.", context);
        Assert.Contains("Chunk two.", context);
    }

    [Fact]
    public async Task GetContextAsync_NoResults_ReturnsEmptyString()
    {
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);

        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoredPoint>());

        var sut = CreateSut();
        var context = await sut.GetContextAsync("query with no results");

        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public async Task GetContextAsync_CallsEmbeddingWithQuery()
    {
        const string query = "tell me about your skills";
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoredPoint>());

        var sut = CreateSut();
        await sut.GetContextAsync(query);

        await _embeddingService.Received(1).GetEmbeddingAsync(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetContextAsync_PassesTopKToSearch()
    {
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoredPoint>());

        var sut = CreateSut();
        await sut.GetContextAsync("query", topK: 3);

        await _vectorStore.Received(1).SearchAsync(Arg.Any<float[]>(), 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetContextAsync_IgnoresResultsWithMissingTextPayload()
    {
        var queryVector = new float[1536];
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);

        var withText = new ScoredPoint { Payload = { ["text"] = new Value { StringValue = "Has text." } } };
        var withoutText = new ScoredPoint(); // no payload

        _vectorStore.SearchAsync(queryVector, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoredPoint> { withText, withoutText });

        var sut = CreateSut();
        var context = await sut.GetContextAsync("query");

        Assert.Contains("Has text.", context);
    }
}
