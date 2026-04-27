using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Tests;

public class DocumentChunkerTests
{
    private readonly DocumentChunker _chunker = new();

    [Fact]
    public void Chunk_EmptyText_ReturnsNoChunks()
    {
        var result = _chunker.Chunk("", "test").ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 10));

        var result = _chunker.Chunk(text, "source.txt").ToList();

        Assert.Single(result);
        Assert.Equal("source.txt", result[0].Source);
        Assert.Equal(0, result[0].ChunkIndex);
    }

    [Fact]
    public void Chunk_LongText_ReturnsMultipleChunks()
    {
        // 1100 words → with chunkSize=500, overlap=50 → should produce 3 chunks
        var text = string.Join(' ', Enumerable.Repeat("word", 1100));

        var result = _chunker.Chunk(text, "doc.txt").ToList();

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void Chunk_EachChunkHasUniqueId()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 1100));

        var result = _chunker.Chunk(text, "doc.txt").ToList();
        var ids = result.Select(c => c.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Chunk_ChunkIndexIsSequential()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 1100));

        var result = _chunker.Chunk(text, "doc.txt").ToList();

        for (var i = 0; i < result.Count; i++)
            Assert.Equal(i, result[i].ChunkIndex);
    }

    [Fact]
    public void Chunk_NoChunkExceedsWordLimit()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 2000));

        var result = _chunker.Chunk(text, "doc.txt", chunkSize: 100, overlap: 10).ToList();

        foreach (var chunk in result)
            Assert.True(chunk.Text.Split(' ').Length <= 100);
    }

    [Fact]
    public void Chunk_SourceIsPreservedOnAllChunks()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 1100));
        const string source = "my-source.pdf";

        var result = _chunker.Chunk(text, source).ToList();

        Assert.All(result, c => Assert.Equal(source, c.Source));
    }
}
