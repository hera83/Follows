using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaRunningModelDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetailsDto? Details { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("sizeVram")]
    public long SizeVram { get; set; }
}
