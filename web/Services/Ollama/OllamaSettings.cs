namespace web.Services.Ollama;

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434/api";
    public string? ApiKey { get; set; }
    public string? DefaultChatModel { get; set; }
    public string? DefaultGenerateModel { get; set; }
    public string? DefaultEmbeddingModel { get; set; }
    public string? DefaultKeepAlive { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool UseStreamingByDefault { get; set; } = false;
    public int RequestTimeoutSeconds { get; set; } = 300;
}
