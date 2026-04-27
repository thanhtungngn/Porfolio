namespace Portfolio.Api.Infrastructure.Persistence.Entities;

public sealed class IngestedFileRecord
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int ChunkCount { get; set; }
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid UploadedByUserId { get; set; }
    public UserAccount? UploadedByUser { get; set; }
}