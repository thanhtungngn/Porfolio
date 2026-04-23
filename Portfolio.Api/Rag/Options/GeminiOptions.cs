public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-004";
    public string EmbeddingEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
}
