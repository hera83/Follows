using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class EmbeddingUsageDto
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }
}
