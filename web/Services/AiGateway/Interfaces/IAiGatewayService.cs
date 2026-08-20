using web.Services.AiGateway.Dtos;
using web.Services.AiGateway.Dtos.Health;
using web.Services.AiGateway.Dtos.Keys;
using web.Services.AiGateway.Dtos.KnowledgeBase;
using web.Services.AiGateway.Dtos.Logs;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Dtos.Speaches;
using SpeachesListModelsResponseDto = web.Services.AiGateway.Dtos.Speaches.ListModelsResponseDto;
using OllamaListModelsResponseDto = web.Services.AiGateway.Dtos.Ollama.ListModelsResponseDto;

namespace web.Services.AiGateway.Interfaces;

public interface IAiGatewayService
{
    Task<HealthResponseDto> CheckHealthAsync(CancellationToken cancellationToken = default);

    Task<CreateKeyResponseDto> CreateKeyAsync(CreateKeyRequestDto request, CancellationToken cancellationToken = default);
    Task<ListKeysResponseDto> ListKeysAsync(CancellationToken cancellationToken = default);
    Task<KeyResponseDto> GetKeyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KeyResponseDto> UpdateKeyAsync(Guid id, UpdateKeyRequestDto request, CancellationToken cancellationToken = default);
    Task<RolloverKeyResponseDto> RolloverKeyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KeyResponseDto> ActivateKeyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KeyResponseDto> DeactivateKeyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuditLogResponseDto> GetKeyAuditLogAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto request, CancellationToken cancellationToken = default);
    Task<ListGroupsResponseDto> ListGroupsAsync(CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentResponseDto> UploadDocumentAsync(UploadDocumentRequestDto request, CancellationToken cancellationToken = default);
    Task<ListDocumentsResponseDto> ListDocumentsAsync(Guid? groupId = null, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiGatewayFileResultDto> DownloadDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SearchResponseDto> SearchKnowledgeBaseAsync(SearchRequestDto request, CancellationToken cancellationToken = default);
    Task<RagChatResponseDto> RagChatAsync(RagChatRequestDto request, CancellationToken cancellationToken = default);

    Task<LogSearchResponseDto> SearchLogsAsync(LogSearchRequestDto request, CancellationToken cancellationToken = default);

    Task<GenerateResponseDto> OllamaGenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<GenerateResponseDto> OllamaGenerateStreamAsync(GenerateRequestDto request, CancellationToken cancellationToken = default);
    Task<ChatResponseDto> OllamaChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatResponseDto> OllamaChatStreamAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    Task<EmbedResponseDto> OllamaEmbedAsync(EmbedRequestDto request, CancellationToken cancellationToken = default);
    Task<OllamaListModelsResponseDto> OllamaListModelsAsync(CancellationToken cancellationToken = default);
    Task<ListRunningModelsResponseDto> OllamaListRunningModelsAsync(CancellationToken cancellationToken = default);
    Task<ShowModelResponseDto> OllamaShowModelAsync(ShowModelRequestDto request, CancellationToken cancellationToken = default);
    Task OllamaCopyModelAsync(CopyModelRequestDto request, CancellationToken cancellationToken = default);
    Task OllamaDeleteModelAsync(DeleteModelRequestDto request, CancellationToken cancellationToken = default);
    Task<PullModelResponseDto> OllamaPullModelAsync(PullModelRequestDto request, CancellationToken cancellationToken = default);
    Task<PushModelResponseDto> OllamaPushModelAsync(PushModelRequestDto request, CancellationToken cancellationToken = default);
    Task<CreateModelResponseDto> OllamaCreateModelAsync(CreateModelRequestDto request, CancellationToken cancellationToken = default);
    Task<VersionResponseDto> OllamaGetVersionAsync(CancellationToken cancellationToken = default);

    Task<ChatCompletionResponseDto> SpeachesChatCompletionsAsync(ChatCompletionRequestDto request, CancellationToken cancellationToken = default);
    Task<TranscriptionResponseDto> SpeachesTranscribeAsync(TranscribeRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// MIDLERTIDIGT DEAKTIVERET - kaster altid <see cref="NotSupportedException"/>. AiGatewayens
    /// forbindelse til Speaches crasher konsekvent lige efter audio-commit, uanset konfiguration.
    /// Se kommentaren ved implementationen i AiGatewayService.Speaches.cs.
    /// </summary>
    Task<AiGatewayRealtimeTranscriptionSession> SpeachesTranscribeRealtimeAsync(string? model = null, string? language = null, CancellationToken cancellationToken = default);
    Task<TranslationResponseDto> SpeachesTranslateAsync(TranslateRequestDto request, CancellationToken cancellationToken = default);
    Task<SpeachesListModelsResponseDto> SpeachesListModelsAsync(string? task = null, CancellationToken cancellationToken = default);
    Task<ListAudioModelsResponseDto> SpeachesListAudioModelsAsync(CancellationToken cancellationToken = default);
    Task<SpeachesListModelsResponseDto> SpeachesListVoicesAsync(CancellationToken cancellationToken = default);
    Task<ModelDto> SpeachesGetModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task SpeachesDownloadModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task SpeachesDeleteModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task<SpeachesListModelsResponseDto> SpeachesGetRegistryAsync(string? task = null, CancellationToken cancellationToken = default);
    Task<RunningModelsResponseDto> SpeachesListRunningModelsAsync(CancellationToken cancellationToken = default);
    Task<MessageResponseDto> SpeachesLoadModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task<MessageResponseDto> SpeachesStopModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task<AiGatewayFileResultDto> SpeachesSynthesizeSpeechAsync(SynthesizeRequestDto request, CancellationToken cancellationToken = default);
    Task<SpeechEmbeddingResponseDto> SpeachesCreateSpeechEmbeddingAsync(CreateSpeechEmbeddingRequestDto request, CancellationToken cancellationToken = default);
    Task<List<SpeechTimestampDto>> SpeachesDetectSpeechTimestampsAsync(DetectSpeechTimestampsRequestDto request, CancellationToken cancellationToken = default);
    Task<DiarizationResponseDto> SpeachesDiarizeAsync(DiarizeRequestDto request, CancellationToken cancellationToken = default);
}
