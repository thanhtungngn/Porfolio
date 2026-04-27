namespace Portfolio.Api.Features.Rag.Options;

public class OpenAiEmbeddingOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string EmbeddingEndpoint { get; set; } = "https://api.openai.com/v1/embeddings";
}
