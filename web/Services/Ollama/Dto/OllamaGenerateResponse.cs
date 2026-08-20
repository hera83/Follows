using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaGenerateResponse : OllamaUsageMetricsDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("thinking")]
    public string? Thinking { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; set; }

    [JsonPropertyName("logprobs")]
    public List<OllamaLogProbDto>? LogProbs { get; set; }
}
