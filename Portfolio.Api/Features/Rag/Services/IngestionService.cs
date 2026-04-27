using Qdrant.Client.Grpc;
using System.Text;
using Portfolio.Api.Infrastructure.Persistence;
using Portfolio.Api.Infrastructure.Persistence.Entities;

namespace Portfolio.Api.Features.Rag.Services;

public class IngestionService(
    PdfDocumentLoader pdfLoader,
    DocumentChunker chunker,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    AppDbContext dbContext,
    ILogger<IngestionService> logger)
{
    private const uint EmbeddingSize = 1536; // OpenAI text-embedding-3-small

    public async Task<int> IngestPdfAsync(string filePath, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting PDF ingestion. FilePath={FilePath}", filePath);
        var text = pdfLoader.LoadText(filePath);
        return await IngestTextAsync(text, source: Path.GetFileName(filePath), cancellationToken);
    }

    public async Task<int> IngestFileAsync(
        Stream stream,
        string fileName,
        string? contentType,
        long sizeBytes,
        Guid uploadedByUserId,
        string? source,
        CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Uploaded file name is required.");
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Authenticated user is required to ingest files.");
        }

        logger.LogInformation("Starting file ingestion. FileName={FileName}, ContentType={ContentType}", fileName, contentType);

        var extension = Path.GetExtension(fileName);
        var sourceName = string.IsNullOrWhiteSpace(source) ? fileName : source.Trim();
        string text;

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            text = pdfLoader.LoadText(stream, fileName);
        }
        else if (IsSupportedTextFile(extension, contentType))
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            text = await reader.ReadToEndAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Unsupported file type. Upload a PDF or text-based document.");
        }

        var chunkCount = await IngestTextAsync(text, source: sourceName, cancellationToken);

        var fileRecord = new IngestedFileRecord
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            SourceName = sourceName,
            ContentType = contentType ?? "application/octet-stream",
            SizeBytes = sizeBytes,
            ChunkCount = chunkCount,
            IngestedAtUtc = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId
        };

        dbContext.Files.Add(fileRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        return chunkCount;
    }

    public async Task<int> IngestTextAsync(string text, string source, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Starting text ingestion. Source={Source}, InputLength={InputLength}",
            source,
            text?.Length ?? 0);

        await vectorStore.EnsureCollectionAsync(EmbeddingSize, cancellationToken);

        var chunks = chunker.Chunk(text, source).ToList();
        logger.LogInformation("Chunking completed. Source={Source}, ChunkCount={ChunkCount}", source, chunks.Count);

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
        logger.LogInformation("Ingestion completed. Source={Source}, UpsertedCount={Count}", source, points.Count);
        return chunks.Count;
    }

    private static bool IsSupportedTextFile(string? extension, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return extension is not null && extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
            || extension is not null && extension.Equals(".yml", StringComparison.OrdinalIgnoreCase);
    }
}