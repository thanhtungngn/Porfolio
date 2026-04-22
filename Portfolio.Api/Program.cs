using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("ChatProviders:OpenAI"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("ChatProviders:Ollama"));
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
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
        new ContactResponse("thanhtungngn@example.com", "https://github.com/thanhtungngn", "https://www.linkedin.com")
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
        return Results.Ok(new ChatResponse(reply));
    }
    catch (ChatValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ChatProviderException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

internal sealed class ChatAgentService(OpenAiChatProvider openAiChatProvider, OllamaChatProvider ollamaChatProvider)
{
    public Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var provider = request.Provider?.Trim().ToLowerInvariant();
        return provider switch
        {
            "openai" => openAiChatProvider.GetReplyAsync(request, cancellationToken),
            "ollama" => ollamaChatProvider.GetReplyAsync(request, cancellationToken),
            _ => throw new ChatValidationException("Provider must be either 'openai' or 'ollama'.")
        };
    }
}

internal sealed class OpenAiChatProvider(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
{
    public async Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new ChatValidationException("OpenAI API key is missing. Configure ChatProviders:OpenAI:ApiKey.");
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
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
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

internal sealed record PortfolioResponse(
    string Name,
    string Role,
    string Summary,
    string[] Technologies,
    string[] Highlights,
    ContactResponse Contact);

internal sealed record ContactResponse(string Email, string GitHub, string LinkedIn);
internal sealed record ChatRequest(string Provider, string Message, string? Model);
internal sealed record ChatResponse(string Reply);
