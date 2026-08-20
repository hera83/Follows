using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ChatCompletionUsageDto
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("promptTokensDetails")]
    public JsonElement? PromptTokensDetails { get; set; }

    [JsonPropertyName("completionTokensDetails")]
    public JsonElement? CompletionTokensDetails { get; set; }
}
