using System.Runtime.CompilerServices;
using System.Text.Json;
using web.Services.AiGateway.Dtos.Ollama;

namespace web.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<GenerateResponseDto> OllamaGenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<GenerateRequestDto, GenerateResponseDto>(client, "Ollama/Generate", request, cancellationToken);
    }

    public IAsyncEnumerable<GenerateResponseDto> OllamaGenerateStreamAsync(GenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        return PostNdjsonStreamAsync<GenerateRequestDto, GenerateResponseDto>("Ollama/Generate", request, cancellationToken);
    }

    public async Task<ChatResponseDto> OllamaChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<ChatRequestDto, ChatResponseDto>(client, "Ollama/Chat", request, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseDto> OllamaChatStreamAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        return PostNdjsonStreamAsync<ChatRequestDto, ChatResponseDto>("Ollama/Chat", request, cancellationToken);
    }

    public async Task<EmbedResponseDto> OllamaEmbedAsync(EmbedRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<EmbedRequestDto, EmbedResponseDto>(client, "Ollama/Embed", request, cancellationToken);
    }

    public async Task<ListModelsResponseDto> OllamaListModelsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ListModelsResponseDto>(client, "Ollama/ListModels", cancellationToken);
    }

    public async Task<ListRunningModelsResponseDto> OllamaListRunningModelsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ListRunningModelsResponseDto>(client, "Ollama/ListRunningModels", cancellationToken);
    }

    public async Task<ShowModelResponseDto> OllamaShowModelAsync(ShowModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<ShowModelRequestDto, ShowModelResponseDto>(client, "Ollama/ShowModel", request, cancellationToken);
    }

    public async Task OllamaCopyModelAsync(CopyModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        await PostNoContentAsync(client, "Ollama/CopyModel", request, cancellationToken);
    }

    public async Task OllamaDeleteModelAsync(DeleteModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        await DeleteWithBodyAsync(client, "Ollama/DeleteModel", request, cancellationToken);
    }

    public async Task<PullModelResponseDto> OllamaPullModelAsync(PullModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<PullModelRequestDto, PullModelResponseDto>(client, "Ollama/PullModel", request, cancellationToken);
    }

    public async Task<PushModelResponseDto> OllamaPushModelAsync(PushModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<PushModelRequestDto, PushModelResponseDto>(client, "Ollama/PushModel", request, cancellationToken);
    }

    public async Task<CreateModelResponseDto> OllamaCreateModelAsync(CreateModelRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<CreateModelRequestDto, CreateModelResponseDto>(client, "Ollama/CreateModel", request, cancellationToken);
    }

    public async Task<VersionResponseDto> OllamaGetVersionAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<VersionResponseDto>(client, "Ollama/GetVersion", cancellationToken);
    }

    // AiGatewayens Ollama-endpoints streamer NDJSON (én JSON-linje pr. chunk) på samme måde som
    // Ollama selv, når stream=true sendes med i requesten - se OllamaService.ChatStreamAsync for
    // det tilsvarende mønster mod Ollama direkte.
    private async IAsyncEnumerable<TResponse> PostNdjsonStreamAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await SendJsonAsync(client, HttpMethod.Post, endpoint, request, cancellationToken, HttpCompletionOption.ResponseHeadersRead);
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

            var chunk = JsonSerializer.Deserialize<TResponse>(line, JsonOptions);
            if (chunk is not null)
                yield return chunk;
        }
    }
}
