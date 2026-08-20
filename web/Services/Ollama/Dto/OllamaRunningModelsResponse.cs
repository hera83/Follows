using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaRunningModelsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaRunningModelDto> Models { get; set; } = new();
}
