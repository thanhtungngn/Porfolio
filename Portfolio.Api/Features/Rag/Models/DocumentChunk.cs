namespace Portfolio.Api.Features.Rag.Models;

public record DocumentChunk(
    Guid Id,
    string Text,
    string Source,
    int ChunkIndex);