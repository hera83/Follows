using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaPushRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("insecure")]
    public bool? Insecure { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
}
