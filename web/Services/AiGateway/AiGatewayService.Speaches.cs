using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using web.Services.AiGateway.Dtos;
using web.Services.AiGateway.Dtos.Speaches;

namespace web.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<ChatCompletionResponseDto> SpeachesChatCompletionsAsync(ChatCompletionRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<ChatCompletionRequestDto, ChatCompletionResponseDto>(client, "Speaches/ChatCompletions", request, cancellationToken);
    }

    public async Task<TranscriptionResponseDto> SpeachesTranscribeAsync(TranscribeRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFormValue(content, "Model", request.Model);
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);
        AddFormValue(content, "Language", request.Language);
        AddFormValue(content, "Prompt", request.Prompt);
        if (request.Temperature.HasValue)
        {
            AddFormValue(content, "Temperature", request.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (request.TimestampGranularities is not null)
        {
            foreach (var granularity in request.TimestampGranularities)
            {
                content.Add(new StringContent(granularity), "TimestampGranularities");
            }
        }
        AddFormValue(content, "Hotwords", request.Hotwords);
        if (request.WithoutTimestamps.HasValue)
        {
            AddFormValue(content, "WithoutTimestamps", request.WithoutTimestamps.Value.ToString());
        }

        return await SendMultipartForJsonAsync<TranscriptionResponseDto>(client, "Speaches/Transcribe", content, cancellationToken);
    }

    // MIDLERTIDIGT DEAKTIVERET: AiGatewayens forbindelse til den bagvedliggende Speaches-tjeneste
    // crasher konsekvent ("The remote party closed the WebSocket connection without completing the
    // close handshake") lige efter input_audio_buffer.commit, uanset turn_detection-konfiguration
    // (afprøvet: server_vad+manuel commit, turn_detection:null (afvises af deres eget skema),
    // turn_detection:{type:"none"}, og ren server-VAD uden manuel commit - alle fejler identisk).
    // Bekræftet af AiGateway-teamet som en fejl i Speaches, ikke i denne klient. Resten af
    // implementationen (protokol, DTO'er, AiGatewayRealtimeTranscriptionSession) er verificeret
    // korrekt mod den rigtige server og kan bruges uændret - fjern blot throw'et herunder, når
    // fejlen er rettet server-side.
    public Task<AiGatewayRealtimeTranscriptionSession> SpeachesTranscribeRealtimeAsync(string? model = null, string? language = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Realtids-transskription er midlertidigt deaktiveret: AiGatewayens forbindelse til Speaches " +
            "crasher lige efter lyd-commit, uanset konfiguration, så der aldrig kommer en transskription " +
            "tilbage. Se kommentaren ved denne metode i AiGatewayService.Speaches.cs.");
    }

    private async Task<AiGatewayRealtimeTranscriptionSession> ConnectRealtimeTranscriptionAsync(string? model, string? language, CancellationToken cancellationToken)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var socket = new ClientWebSocket();
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            socket.Options.SetRequestHeader("X-Api-Key", config.ApiKey);
        }

        var uri = BuildRealtimeTranscribeUri(config.BaseUrl, model, language);
        try
        {
            await socket.ConnectAsync(uri, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            socket.Dispose();
            throw new AiGatewayException(HttpStatusCode.BadGateway, $"Kunne ikke oprette realtids-transskriptionsforbindelse til AiGateway: {ex.Message}");
        }

        return new AiGatewayRealtimeTranscriptionSession(socket);
    }

    private static Uri BuildRealtimeTranscribeUri(string baseUrl, string? model, string? language)
    {
        var httpUri = new Uri(baseUrl.TrimEnd('/') + "/Speaches/TranscribeRealtime", UriKind.Absolute);
        var wsScheme = httpUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        var builder = new UriBuilder(httpUri) { Scheme = wsScheme, Port = httpUri.IsDefaultPort ? -1 : httpUri.Port };

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(model))
            query.Add($"Model={Uri.EscapeDataString(model)}");
        if (!string.IsNullOrWhiteSpace(language))
            query.Add($"Language={Uri.EscapeDataString(language)}");
        builder.Query = string.Join("&", query);

        return builder.Uri;
    }

    public async Task<TranslationResponseDto> SpeachesTranslateAsync(TranslateRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFormValue(content, "Model", request.Model);
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);
        AddFormValue(content, "Prompt", request.Prompt);
        if (request.Temperature.HasValue)
        {
            AddFormValue(content, "Temperature", request.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return await SendMultipartForJsonAsync<TranslationResponseDto>(client, "Speaches/Translate", content, cancellationToken);
    }

    public async Task<ListModelsResponseDto> SpeachesListModelsAsync(string? task = null, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var endpoint = "Speaches/ListModels";
        if (!string.IsNullOrWhiteSpace(task))
        {
            endpoint += $"?Task={Uri.EscapeDataString(task)}";
        }

        return await GetAsync<ListModelsResponseDto>(client, endpoint, cancellationToken);
    }

    public async Task<ListAudioModelsResponseDto> SpeachesListAudioModelsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ListAudioModelsResponseDto>(client, "Speaches/ListAudioModels", cancellationToken);
    }

    public async Task<ListModelsResponseDto> SpeachesListVoicesAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ListModelsResponseDto>(client, "Speaches/ListVoices", cancellationToken);
    }

    public async Task<ModelDto> SpeachesGetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ModelDto>(client, $"Speaches/GetModel/{Uri.EscapeDataString(modelId)}", cancellationToken);
    }

    public async Task SpeachesDownloadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.PostAsync($"Speaches/DownloadModel/{Uri.EscapeDataString(modelId)}", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task SpeachesDeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        await DeleteAsync(client, $"Speaches/DeleteModel/{Uri.EscapeDataString(modelId)}", cancellationToken);
    }

    public async Task<ListModelsResponseDto> SpeachesGetRegistryAsync(string? task = null, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var endpoint = "Speaches/GetRegistry";
        if (!string.IsNullOrWhiteSpace(task))
        {
            endpoint += $"?Task={Uri.EscapeDataString(task)}";
        }

        return await GetAsync<ListModelsResponseDto>(client, endpoint, cancellationToken);
    }

    public async Task<RunningModelsResponseDto> SpeachesListRunningModelsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<RunningModelsResponseDto>(client, "Speaches/ListRunningModels", cancellationToken);
    }

    public async Task<MessageResponseDto> SpeachesLoadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<MessageResponseDto>(client, $"Speaches/LoadModel/{Uri.EscapeDataString(modelId)}", cancellationToken);
    }

    public async Task<MessageResponseDto> SpeachesStopModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.DeleteAsync($"Speaches/StopModel/{Uri.EscapeDataString(modelId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MessageResponseDto>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }

    public async Task<AiGatewayFileResultDto> SpeachesSynthesizeSpeechAsync(SynthesizeRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);
        var response = await SendJsonAsync(client, HttpMethod.Post, "Speaches/SynthesizeSpeech", request, cancellationToken, HttpCompletionOption.ResponseHeadersRead);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ToFileResultAsync(response, cancellationToken);
    }

    public async Task<SpeechEmbeddingResponseDto> SpeachesCreateSpeechEmbeddingAsync(CreateSpeechEmbeddingRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFormValue(content, "Model", request.Model);
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);

        return await SendMultipartForJsonAsync<SpeechEmbeddingResponseDto>(client, "Speaches/CreateSpeechEmbedding", content, cancellationToken);
    }

    public async Task<List<SpeechTimestampDto>> SpeachesDetectSpeechTimestampsAsync(DetectSpeechTimestampsRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);
        AddFormValue(content, "Model", request.Model);
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        if (request.Threshold.HasValue) AddFormValue(content, "Threshold", request.Threshold.Value.ToString(culture));
        if (request.NegThreshold.HasValue) AddFormValue(content, "NegThreshold", request.NegThreshold.Value.ToString(culture));
        if (request.MinSpeechDurationMs.HasValue) AddFormValue(content, "MinSpeechDurationMs", request.MinSpeechDurationMs.Value.ToString(culture));
        if (request.MaxSpeechDurationS.HasValue) AddFormValue(content, "MaxSpeechDurationS", request.MaxSpeechDurationS.Value.ToString(culture));
        if (request.MinSilenceDurationMs.HasValue) AddFormValue(content, "MinSilenceDurationMs", request.MinSilenceDurationMs.Value.ToString(culture));
        if (request.SpeechPadMs.HasValue) AddFormValue(content, "SpeechPadMs", request.SpeechPadMs.Value.ToString(culture));

        return await SendMultipartForJsonAsync<List<SpeechTimestampDto>>(client, "Speaches/DetectSpeechTimestamps", content, cancellationToken);
    }

    public async Task<DiarizationResponseDto> SpeachesDiarizeAsync(DiarizeRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);
        if (request.KnownSpeakerNames is not null)
        {
            foreach (var name in request.KnownSpeakerNames)
            {
                content.Add(new StringContent(name), "KnownSpeakerNames");
            }
        }
        if (request.KnownSpeakerReferences is not null)
        {
            foreach (var reference in request.KnownSpeakerReferences)
            {
                content.Add(new StringContent(reference), "KnownSpeakerReferences");
            }
        }

        return await SendMultipartForJsonAsync<DiarizationResponseDto>(client, "Speaches/Diarize", content, cancellationToken);
    }

    private static async Task<TResponse> SendMultipartForJsonAsync<TResponse>(HttpClient client, string endpoint, MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        using var response = await SendMultipartAsync(client, HttpMethod.Post, endpoint, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }
}
