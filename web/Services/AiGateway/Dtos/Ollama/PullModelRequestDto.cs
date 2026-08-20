using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class PullModelRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("insecure")]
    public bool? Insecure { get; set; }
}
