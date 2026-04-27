using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Chat.Contracts;
using Portfolio.Api.Features.Chat.Exceptions;
using Portfolio.Api.Features.Chat.Options;

namespace Portfolio.Api.Features.Chat.Services;

internal sealed class OllamaChatProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OllamaOptions> options) : IChatProvider
{
    public string Name => "ollama";

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
