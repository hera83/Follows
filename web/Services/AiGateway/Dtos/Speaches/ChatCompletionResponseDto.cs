using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ChatCompletionResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<ChatCompletionChoiceDto>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public ChatCompletionUsageDto? Usage { get; set; }

    [JsonPropertyName("systemFingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("serviceTier")]
    public string? ServiceTier { get; set; }
}
