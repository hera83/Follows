using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class EmbeddingObjectDto
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public List<double>? Embedding { get; set; }
}
