using web.Services.Ollama.Dto;

namespace web.Services.Ollama.Interfaces;

public interface IOllamaService
{
    Task<OllamaGenerateResponse> GenerateAsync(OllamaGenerateRequest request, CancellationToken cancellationToken = default);
    Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OllamaChatResponse> ChatStreamAsync(OllamaChatRequest request, CancellationToken cancellationToken = default);
    Task<OllamaEmbedResponse> EmbedAsync(OllamaEmbedRequest request, CancellationToken cancellationToken = default);
    Task<OllamaTagsResponse> ListModelsAsync(CancellationToken cancellationToken = default);
    Task<OllamaRunningModelsResponse> ListRunningModelsAsync(CancellationToken cancellationToken = default);
    Task<OllamaShowResponse> ShowModelAsync(OllamaShowRequest request, CancellationToken cancellationToken = default);
    Task<OllamaStatusResponse> CreateModelAsync(OllamaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> CopyModelAsync(OllamaCopyRequest request, CancellationToken cancellationToken = default);
    Task<OllamaStatusResponse> PullModelAsync(OllamaPullRequest request, CancellationToken cancellationToken = default);
    Task<OllamaStatusResponse> PushModelAsync(OllamaPushRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteModelAsync(OllamaDeleteRequest request, CancellationToken cancellationToken = default);
    Task<OllamaVersionResponse> GetVersionAsync(CancellationToken cancellationToken = default);
}
