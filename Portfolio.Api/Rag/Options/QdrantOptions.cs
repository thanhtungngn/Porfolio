public class QdrantOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CollectionName { get; set; } = "portfolio";
    public string? ApiKey { get; set; }
    public bool Https { get; set; } = true;
}
