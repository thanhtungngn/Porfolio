namespace Portfolio.Api.Features.Rag.Contracts;

public sealed record RagSearchRequest(string Query, int? TopK);
