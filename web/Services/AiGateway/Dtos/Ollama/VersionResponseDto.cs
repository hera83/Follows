using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class VersionResponseDto
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
