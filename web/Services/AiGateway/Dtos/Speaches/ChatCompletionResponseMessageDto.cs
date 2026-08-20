using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ChatCompletionResponseMessageDto
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }

    [JsonPropertyName("toolCalls")]
    public JsonElement? ToolCalls { get; set; }

    [JsonPropertyName("audio")]
    public JsonElement? Audio { get; set; }

    [JsonPropertyName("annotations")]
    public JsonElement? Annotations { get; set; }
}
