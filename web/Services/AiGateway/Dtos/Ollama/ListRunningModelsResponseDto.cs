using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class ListRunningModelsResponseDto
{
    [JsonPropertyName("models")]
    public List<OllamaRunningModelDto>? Models { get; set; }
}
