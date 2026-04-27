using Portfolio.Api.Features.Chat.Contracts;
using Portfolio.Api.Features.Chat.Exceptions;
using Portfolio.Api.Features.Chat.Services;

namespace Portfolio.Api.Features.Chat.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/chat", async (ChatRequest request, ChatAgentService chatAgentService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            if (request.UseRag == true)
            {
                return Results.Unauthorized();
            }

            try
            {
                var reply = await chatAgentService.GetReplyAsync(request, cancellationToken);
                return Results.Ok(reply);
            }
            catch (ChatValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ChatProviderException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireRateLimiting("chat");

        endpoints.MapPost("/api/chat/rag", async (ChatRequest request, ChatAgentService chatAgentService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            var ragRequest = request with { UseRag = true };

            try
            {
                var reply = await chatAgentService.GetReplyAsync(ragRequest, cancellationToken);
                return Results.Ok(reply);
            }
            catch (ChatValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ChatProviderException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization().RequireRateLimiting("chat");

        return endpoints;
    }
}
