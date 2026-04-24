using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using System.Text;
using System.Text.Json;
using Grpc.Core;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.Configure<ApiSecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("ChatProviders:OpenAI"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("ChatProviders:Ollama"));
builder.Services.Configure<OpenAiEmbeddingOptions>(builder.Configuration.GetSection("Rag:OpenAiEmbedding"));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Rag:Qdrant"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("chat", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
builder.Services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddScoped<DocumentChunker>();
builder.Services.AddScoped<PdfDocumentLoader>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RagRetrievalService>();
builder.Services.AddScoped<OpenAiChatProvider>();
builder.Services.AddScoped<OllamaChatProvider>();
builder.Services.AddScoped<ChatAgentService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request");
    var start = DateTime.UtcNow;

    await next();

    var elapsed = DateTime.UtcNow - start;
    logger.LogInformation(
        "Request completed {Method} {Path} with {StatusCode} in {ElapsedMs}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        elapsed.TotalMilliseconds);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Portfolio RAG API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseCors("frontend");
app.UseRateLimiter();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/api/portfolio", () =>
{
    var info = new PortfolioResponse(
        "Thanh Tung Nguyen",
        "Software Engineer",
        "I build practical full-stack products and AI-assisted workflows.",
        [".NET 10 / ASP.NET", "React", "Cloud Automation", "LLM Agents (OpenAI + Ollama)"],
        [
            "Develop scalable backend services with ASP.NET.",
            "Create responsive UI experiences in React.",
            "Integrate AI copilots that explain implementation details on demand."
        ],
        new ContactResponse("thanhtungngn@example.com", "https://github.com/thanhtungngn", "https://www.linkedin.com/in/thanhtungngn")
    );

    return TypedResults.Ok(info);
});

app.MapPost("/api/chat", async (ChatRequest request, ChatAgentService chatAgentService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
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

app.MapPost("/api/rag/ingest", async (
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
    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unimplemented || ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
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

app.MapPost("/api/rag/search", async (RagSearchRequest request, RagRetrievalService ragService, CancellationToken cancellationToken) =>
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

app.Run();

internal sealed class ChatAgentService(
    OpenAiChatProvider openAiChatProvider,
    OllamaChatProvider ollamaChatProvider,
    RagRetrievalService ragRetrievalService)
{
    public async Task<ChatResponse> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var augmentedRequest = request;
        IReadOnlyList<RagSource> sources = [];

        if (request.UseRag == true && !string.IsNullOrWhiteSpace(request.Message))
        {
            var matches = await ragRetrievalService.GetMatchesAsync(request.Message, cancellationToken: cancellationToken);
            if (matches.Count > 0)
            {
                var context = string.Join("\n\n", matches.Select(m => m.Text));
                var augmentedMessage = $"Use the following context to answer the question.\n\nContext:\n{context}\n\nQuestion:\n{request.Message}";
                augmentedRequest = request with { Message = augmentedMessage };

                sources = matches
                    .Where(m => !string.IsNullOrWhiteSpace(m.Source))
                    .Select(m => new RagSource(m.Source, m.Score))
                    .GroupBy(s => s.Source, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.Score).First())
                    .OrderByDescending(s => s.Score)
                    .ToList();
            }
        }

        var provider = augmentedRequest.Provider?.Trim().ToLowerInvariant();
        var reply = provider switch
        {
            "openai" => await openAiChatProvider.GetReplyAsync(augmentedRequest, cancellationToken),
            "ollama" => await ollamaChatProvider.GetReplyAsync(augmentedRequest, cancellationToken),
            _ => throw new ChatValidationException("Provider must be either 'openai' or 'ollama'.")
        };

        return new ChatResponse(reply, sources);
    }
}

internal sealed class OpenAiChatProvider(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
{
    public async Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;
            var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
                ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                : settings.ApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ChatValidationException("OpenAI API key is missing. Configure OPENAI_API_KEY or ChatProviders:OpenAI:ApiKey.");
            }

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(request.Model) ? settings.DefaultModel : request.Model,
                messages = new[]
                {
                    new { role = "system", content = settings.SystemPrompt },
                    new { role = "user", content = request.Message }
                }
            };

            var client = httpClientFactory.CreateClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(requestMessage, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ChatProviderException($"OpenAI request failed with status {(int)response.StatusCode}: {json}");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ChatProviderException("OpenAI response did not include a valid reply.");
            }

            return content;
        }
        catch (HttpRequestException)
        {
            throw new ChatProviderException("OpenAI endpoint is unreachable. Check network and configuration.");
        }
        catch (JsonException)
        {
            throw new ChatProviderException("OpenAI response format was invalid.");
        }
    }
}

internal sealed class OllamaChatProvider(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
{
    public async Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(request.Model) ? settings.DefaultModel : request.Model,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = settings.SystemPrompt },
                    new { role = "user", content = request.Message }
                }
            };

            var client = httpClientFactory.CreateClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(requestMessage, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ChatProviderException($"Ollama request failed with status {(int)response.StatusCode}: {json}");
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ChatProviderException("Ollama response did not include a valid reply.");
            }

            return content;
        }
        catch (HttpRequestException)
        {
            throw new ChatProviderException("Ollama endpoint is unreachable. Ensure Ollama is running.");
        }
        catch (JsonException)
        {
            throw new ChatProviderException("Ollama response format was invalid.");
        }
    }
}

internal sealed record IngestRequest(string? FilePath, string? Text, string? Source);

internal sealed class ChatValidationException(string message) : Exception(message);
internal sealed class ChatProviderException(string message) : Exception(message);

internal sealed class OpenAiOptions
{
    public string Endpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
    public string DefaultModel { get; init; } = "gpt-4o-mini";
    public string SystemPrompt { get; init; } = "You are an assistant for a portfolio website. Be concise and practical.";
    public string ApiKey { get; init; } = string.Empty;
}

internal sealed class OllamaOptions
{
    public string Endpoint { get; init; } = "http://localhost:11434/api/chat";
    public string DefaultModel { get; init; } = "llama3.2";
    public string SystemPrompt { get; init; } = "You are an assistant for a portfolio website. Be concise and practical.";
}

internal sealed class ApiSecurityOptions
{
    public string IngestApiKey { get; init; } = string.Empty;
}

internal sealed record PortfolioResponse(
    string Name,
    string Role,
    string Summary,
    string[] Technologies,
    string[] Highlights,
    ContactResponse Contact);

internal sealed record ContactResponse(string Email, string GitHub, string LinkedIn);
internal sealed record ChatRequest(string Provider, string Message, string? Model, bool? UseRag = false);
internal sealed record ChatResponse(string Reply, IReadOnlyList<RagSource>? Sources = null);
internal sealed record RagSource(string Source, float Score);
internal sealed record RagSearchRequest(string Query, int? TopK);
internal sealed record RagSearchResponse(string Context, IReadOnlyList<RagMatch> Matches);
