public record DocumentChunk(
    Guid Id,
    string Text,
    string Source,
    int ChunkIndex);