using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class SpeechEmbeddingResponseDto
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public List<EmbeddingObjectDto>? Data { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("usage")]
    public EmbeddingUsageDto? Usage { get; set; }
}
