using Grpc.Core;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Features.Rag.Contracts;
using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Api.Features.Rag.Endpoints;

public static class RagEndpoints
{
    public static IEndpointRouteBuilder MapRagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rag = endpoints.MapGroup("/api/rag").RequireAuthorization();

        rag.MapPost("/ingest", async (
            [FromForm] IngestFormRequest request,
            IngestionService ingestionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var hasText = !string.IsNullOrWhiteSpace(request.Text);
            var hasFile = request.File is not null && request.File.Length > 0;

            if (!hasText && !hasFile)
                return Results.BadRequest(new { error = "Provide either 'text' or an uploaded file." });

            try
            {
                int count;

                if (hasFile)
                {
                    var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(userIdValue, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    await using var stream = request.File!.OpenReadStream();
                    count = await ingestionService.IngestFileAsync(
                        stream,
                        request.File.FileName,
                        request.File.ContentType,
                        request.File.Length,
                        userId,
                        request.Source,
                        cancellationToken);
                }
                else
                {
                    count = await ingestionService.IngestTextAsync(request.Text!, request.Source ?? "manual", cancellationToken);
                }

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
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).DisableAntiforgery();

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
