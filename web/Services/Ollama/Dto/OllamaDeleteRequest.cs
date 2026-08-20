using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaDeleteRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}
