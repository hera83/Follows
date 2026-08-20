using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ChatCompletionMessageDto
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public JsonElement? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("toolCalls")]
    public JsonElement? ToolCalls { get; set; }

    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }
}
