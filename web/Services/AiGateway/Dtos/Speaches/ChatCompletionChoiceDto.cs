using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ChatCompletionChoiceDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ChatCompletionResponseMessageDto? Message { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    public JsonElement? Logprobs { get; set; }
}
