using Grpc.Core;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Rag.Contracts;
using Portfolio.Api.Features.Rag.Services;
using Portfolio.Api.Infrastructure.Extensions;

namespace Portfolio.Api.Features.Rag.Endpoints;

public static class RagEndpoints
{
    public static IEndpointRouteBuilder MapRagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rag = endpoints.MapGroup("/api/rag");

        rag.MapPost("/ingest", async (
            IngestRequest request,
            IngestionService ingestionService,
            IOptions<ApiSecurityOptions> securityOptions,
            HttpRequest httpRequest,
            CancellationToken cancellationToken) =>
        {
            var expectedApiKey = string.IsNullOrWhiteSpace(securityOptions.Value.IngestApiKey)
                ? Environment.GetEnvironmentVariable("INGEST_API_KEY")
                : securityOptions.Value.IngestApiKey;
            var providedApiKey = httpRequest.Headers["X-API-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(expectedApiKey) || !string.Equals(expectedApiKey, providedApiKey, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.FilePath))
                return Results.BadRequest(new { error = "Provide either 'text' or 'filePath'." });

            try
            {
                var count = !string.IsNullOrWhiteSpace(request.FilePath)
                    ? await ingestionService.IngestPdfAsync(request.FilePath!, cancellationToken)
                    : await ingestionService.IngestTextAsync(request.Text!, request.Source ?? "manual", cancellationToken);

                return Results.Ok(new { chunksIngested = count });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented || ex.StatusCode == StatusCode.Unavailable)
            {
                return Results.Problem(
                    "Cannot connect to Qdrant. Make sure Qdrant is running: docker run -d -p 6333:6333 -p 6334:6334 qdrant/qdrant",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("OpenAI"))
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        rag.MapPost("/search", async (RagSearchRequest request, RagRetrievalService ragService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return Results.BadRequest(new { error = "Query is required." });

            try
            {
                var matches = await ragService.GetMatchesAsync(request.Query, request.TopK ?? 5, cancellationToken);
                var context = string.Join("\n\n", matches.Select(m => m.Text));
                return Results.Ok(new RagSearchResponse(context, matches));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("OpenAI"))
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented || ex.StatusCode == StatusCode.Unavailable)
            {
                return Results.Problem(
                    "Cannot connect to Qdrant. Make sure Qdrant is running.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        return endpoints;
    }
}
