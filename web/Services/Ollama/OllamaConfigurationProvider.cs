using web.Services.Ollama.Interfaces;

namespace web.Services.Ollama;

public class OllamaConfigurationProvider : IOllamaConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public OllamaConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<OllamaSettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var settings = new OllamaSettings
        {
            BaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/api",
            ApiKey = _configuration["Ollama:ApiKey"],
            UseStreamingByDefault = bool.TryParse(_configuration["Ollama:UseStreamingByDefault"], out var streaming) && streaming,
            RequestTimeoutSeconds = int.TryParse(_configuration["Ollama:RequestTimeoutSeconds"], out var timeout) && timeout > 0 ? timeout : 300,
            DefaultChatModel = _configuration["Ollama:DefaultChatModel"],
            DefaultGenerateModel = _configuration["Ollama:DefaultGenerateModel"],
            DefaultEmbeddingModel = _configuration["Ollama:DefaultEmbeddingModel"],
            DefaultKeepAlive = _configuration["Ollama:DefaultKeepAlive"],
            DefaultLanguage = _configuration["Ollama:DefaultLanguage"],
        };

        return Task.FromResult(settings);
    }
}
