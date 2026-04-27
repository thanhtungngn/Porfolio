using Microsoft.AspNetCore.Http;

namespace Portfolio.Api.Features.Rag.Contracts;

public sealed class IngestFormRequest
{
	public IFormFile? File { get; init; }

	public string? Text { get; init; }

	public string? Source { get; init; }
}