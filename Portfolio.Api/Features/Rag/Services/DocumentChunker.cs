using Portfolio.Api.Features.Rag.Models;

namespace Portfolio.Api.Features.Rag.Services;

public class DocumentChunker
{
    private const int DefaultChunkSize = 500;
    private const int DefaultOverlap = 50;

    public IEnumerable<DocumentChunk> Chunk(
        string text,
        string source,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultOverlap)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var index = 0;
        var chunkIndex = 0;

        while (index < words.Length)
        {
            var end = Math.Min(index + chunkSize, words.Length);
            var chunkText = string.Join(' ', words[index..end]);

            yield return new DocumentChunk(Guid.NewGuid(), chunkText, source, chunkIndex++);

            index += chunkSize - overlap;
        }
    }
}