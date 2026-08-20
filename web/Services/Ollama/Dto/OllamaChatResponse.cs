using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaChatResponse : OllamaUsageMetricsDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public OllamaChatMessageDto Message { get; set; } = new();

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; set; }

    [JsonPropertyName("logprobs")]
    public List<OllamaLogProbDto>? LogProbs { get; set; }
}
