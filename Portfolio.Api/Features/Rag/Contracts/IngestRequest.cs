namespace Portfolio.Api.Features.Rag.Contracts;

internal sealed record IngestRequest(string? FilePath, string? Text, string? Source);
