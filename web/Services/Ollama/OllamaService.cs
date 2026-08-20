using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using web.Services.Ollama.Dto;
using web.Services.Ollama.Interfaces;

namespace web.Services.Ollama;

public class OllamaService : IOllamaService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IOllamaConfigurationProvider _configurationProvider;
    private readonly OllamaHttpClientFactory _httpClientFactory;

    public OllamaService(
        IOllamaConfigurationProvider configurationProvider,
        OllamaHttpClientFactory httpClientFactory)
    {
        _configurationProvider = configurationProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OllamaGenerateResponse> GenerateAsync(OllamaGenerateRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream ??= false;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaGenerateRequest, OllamaGenerateResponse>(client, "generate", request, cancellationToken);
    }

    public async Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream ??= false;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaChatRequest, OllamaChatResponse>(client, "chat", request, cancellationToken);
    }

    public async IAsyncEnumerable<OllamaChatResponse> ChatStreamAsync(OllamaChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        using var response = await SendJsonAsync(client, HttpMethod.Post, "chat", request, cancellationToken, HttpCompletionOption.ResponseHeadersRead);
        await EnsureSuccessAsync(response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
            if (chunk is not null)
                yield return chunk;
        }
    }

    public async Task<OllamaEmbedResponse> EmbedAsync(OllamaEmbedRequest request, CancellationToken cancellationToken = default)
    {
        request.Truncate ??= true;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaEmbedRequest, OllamaEmbedResponse>(client, "embed", request, cancellationToken);
    }

    public async Task<OllamaTagsResponse> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await GetAsync<OllamaTagsResponse>(client, "tags", cancellationToken);
    }

    public async Task<OllamaRunningModelsResponse> ListRunningModelsAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await GetAsync<OllamaRunningModelsResponse>(client, "ps", cancellationToken);
    }

    public async Task<OllamaShowResponse> ShowModelAsync(OllamaShowRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaShowRequest, OllamaShowResponse>(client, "show", request, cancellationToken);
    }

    public async Task<OllamaStatusResponse> CreateModelAsync(OllamaCreateRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream ??= false;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaCreateRequest, OllamaStatusResponse>(client, "create", request, cancellationToken);
    }

    public async Task<bool> CopyModelAsync(OllamaCopyRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        using var response = await SendJsonAsync(client, HttpMethod.Post, "copy", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public async Task<OllamaStatusResponse> PullModelAsync(OllamaPullRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream ??= false;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaPullRequest, OllamaStatusResponse>(client, "pull", request, cancellationToken);
    }

    public async Task<OllamaStatusResponse> PushModelAsync(OllamaPushRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream ??= false;
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await PostAsync<OllamaPushRequest, OllamaStatusResponse>(client, "push", request, cancellationToken);
    }

    public async Task<bool> DeleteModelAsync(OllamaDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        using var response = await SendJsonAsync(client, HttpMethod.Delete, "delete", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public async Task<OllamaVersionResponse> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        using var client = _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
        return await GetAsync<OllamaVersionResponse>(client, "version", cancellationToken);
    }

    private static async Task<TResponse> GetAsync<TResponse>(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new OllamaException(HttpStatusCode.InternalServerError, "Kunne ikke parse Ollama response.");
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(client, HttpMethod.Post, endpoint, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new OllamaException(HttpStatusCode.InternalServerError, "Kunne ikke parse Ollama response.");
    }

    private static async Task<HttpResponseMessage> SendJsonAsync<TRequest>(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var message = new HttpRequestMessage(method, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await client.SendAsync(message, completionOption, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        string message = $"Ollama request fejlede med status {(int)response.StatusCode}.";

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var error = JsonSerializer.Deserialize<OllamaErrorResponse>(raw, JsonOptions);
                if (!string.IsNullOrWhiteSpace(error?.Error))
                {
                    message = error.Error;
                }
                else
                {
                    message = raw;
                }
            }
            catch
            {
                message = raw;
            }
        }

        throw new OllamaException(response.StatusCode, message);
    }
}
