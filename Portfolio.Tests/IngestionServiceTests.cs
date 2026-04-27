using NSubstitute;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Features.Rag.Services;
using System.Text;
using Portfolio.Api.Infrastructure.Persistence;
using Portfolio.Api.Infrastructure.Persistence.Entities;

namespace Portfolio.Tests;

public class IngestionServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly PdfDocumentLoader _pdfLoader       = new(Substitute.For<ILogger<PdfDocumentLoader>>());
    private readonly DocumentChunker   _chunker         = new();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore      _vectorStore      = Substitute.For<IVectorStore>();
    private readonly ILogger<IngestionService> _logger = Substitute.For<ILogger<IngestionService>>();

    public IngestionServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
    }

    private IngestionService CreateSut() =>
        new(_pdfLoader, _chunker, _embeddingService, _vectorStore, _dbContext, _logger);

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

    [Fact]
    public async Task IngestFileAsync_TextFile_UsesUploadedFileNameAsSource()
    {
        var text = string.Join(' ', Enumerable.Repeat("knowledge", 40));
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var fakeEmbedding = new float[1536];
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Username = "tester",
            PasswordHash = "hash"
        };
        IEnumerable<PointStruct>? upsertedPoints = null;

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fakeEmbedding);

        _vectorStore
            .When(store => store.UpsertAsync(Arg.Any<IEnumerable<PointStruct>>(), Arg.Any<CancellationToken>()))
            .Do(call => upsertedPoints = call.Arg<IEnumerable<PointStruct>>());

        var sut = CreateSut();
        var count = await sut.IngestFileAsync(fileStream, "notes.txt", "text/plain", text.Length, user.Id, null);

        Assert.True(count >= 1);
        Assert.NotNull(upsertedPoints);
        Assert.All(upsertedPoints!, point => Assert.Equal("notes.txt", point.Payload["source"].StringValue));

        var savedFile = await _dbContext.Files.SingleAsync();
        Assert.Equal("notes.txt", savedFile.FileName);
        Assert.Equal(user.Id, savedFile.UploadedByUserId);
        Assert.Equal(count, savedFile.ChunkCount);
    }

    [Fact]
    public async Task IngestFileAsync_UnsupportedFileType_ThrowsValidationError()
    {
        var fileStream = new MemoryStream([1, 2, 3, 4]);
        var sut = CreateSut();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.IngestFileAsync(fileStream, "archive.zip", "application/zip", 4, Guid.NewGuid(), null));

        Assert.Equal("Unsupported file type. Upload a PDF or text-based document.", error.Message);
        await _vectorStore.DidNotReceive().UpsertAsync(Arg.Any<IEnumerable<PointStruct>>(), Arg.Any<CancellationToken>());
    }
}
