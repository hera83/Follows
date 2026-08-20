using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class ModelDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("ownedBy")]
    public string? OwnedBy { get; set; }

    [JsonPropertyName("language")]
    public List<string>? Language { get; set; }

    [JsonPropertyName("task")]
    public string? Task { get; set; }
}
