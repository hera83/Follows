using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaCopyRequest
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;
}
