using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Chat.Contracts;
using Portfolio.Api.Features.Chat.Exceptions;
using Portfolio.Api.Features.Chat.Options;

namespace Portfolio.Api.Features.Chat.Services;

internal sealed class OpenAiChatProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiOptions> options) : IChatProvider
{
    public string Name => "openai";

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
