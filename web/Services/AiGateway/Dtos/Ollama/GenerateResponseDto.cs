using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class GenerateResponseDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("doneReason")]
    public string? DoneReason { get; set; }

    [JsonPropertyName("context")]
    public List<long>? Context { get; set; }

    [JsonPropertyName("totalDuration")]
    public long? TotalDuration { get; set; }

    [JsonPropertyName("loadDuration")]
    public long? LoadDuration { get; set; }

    [JsonPropertyName("promptEvalCount")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("promptEvalDuration")]
    public long? PromptEvalDuration { get; set; }

    [JsonPropertyName("evalCount")]
    public int? EvalCount { get; set; }

    [JsonPropertyName("evalDuration")]
    public long? EvalDuration { get; set; }
}
