using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class CopyModelRequestDto
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
}
