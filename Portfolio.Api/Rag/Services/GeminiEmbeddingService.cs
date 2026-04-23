using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class GeminiEmbeddingService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            : settings.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Gemini API key is missing. Set GEMINI_API_KEY or Rag:Gemini:ApiKey.");

        var url = $"{settings.EmbeddingEndpoint}/{settings.EmbeddingModel}:embedContent?key={apiKey}";

        var payload = new
        {
            model = $"models/{settings.EmbeddingModel}",
            content = new { parts = new[] { new { text } } }
        };

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini embedding request failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var values = doc.RootElement
            .GetProperty("embedding")
            .GetProperty("values");

        return values.EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
    }
}
