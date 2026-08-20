using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class RagChatResponseDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public ChatMessageDto? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("doneReason")]
    public string? DoneReason { get; set; }

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

    [JsonPropertyName("sources")]
    public List<ChunkMatchResponseDto>? Sources { get; set; }
}
