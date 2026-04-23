using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public class OpenAiEmbeddingService(IHttpClientFactory httpClientFactory, IOptions<OpenAiEmbeddingOptions> options) : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : settings.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is missing. Set OPENAI_API_KEY or Rag:OpenAiEmbedding:ApiKey.");

        var payload = new
        {
            input = text,
            model = settings.EmbeddingModel
        };

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.EmbeddingEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI embedding request failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var values = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        return values.EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
    }
}
