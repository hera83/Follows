using System.Net.Http.Headers;

namespace web.Services.Ollama;

public class OllamaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient Create(string baseUrl, string? apiKey, int requestTimeoutSeconds)
    {
        var client = _httpClientFactory.CreateClient("Ollama");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds > 0 ? requestTimeoutSeconds : 300);

        client.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        return client;
    }
}
