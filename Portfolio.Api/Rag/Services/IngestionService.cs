using Qdrant.Client.Grpc;

public class IngestionService(
    PdfDocumentLoader pdfLoader,
    DocumentChunker chunker,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore)
{
    private const uint EmbeddingSize = 1536; // OpenAI text-embedding-3-small

    public async Task<int> IngestPdfAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var text = pdfLoader.LoadText(filePath);
        return await IngestTextAsync(text, source: Path.GetFileName(filePath), cancellationToken);
    }

    public async Task<int> IngestTextAsync(string text, string source, CancellationToken cancellationToken = default)
    {
        await vectorStore.EnsureCollectionAsync(EmbeddingSize, cancellationToken);

        var chunks = chunker.Chunk(text, source).ToList();
        var points = new List<PointStruct>();

        foreach (var chunk in chunks)
        {
            var embedding = await embeddingService.GetEmbeddingAsync(chunk.Text, cancellationToken);

            var point = new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = new Vectors { Vector = new Vector { Data = { embedding } } },
                Payload =
                {
                    ["text"] = new Value { StringValue = chunk.Text },
                    ["source"] = new Value { StringValue = chunk.Source },
                    ["chunkIndex"] = new Value { IntegerValue = chunk.ChunkIndex }
                }
            };

            points.Add(point);
        }

        await vectorStore.UpsertAsync(points, cancellationToken);
        return chunks.Count;
    }
}