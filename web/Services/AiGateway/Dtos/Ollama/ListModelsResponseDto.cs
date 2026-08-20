using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class ListModelsResponseDto
{
    [JsonPropertyName("models")]
    public List<OllamaModelSummaryDto>? Models { get; set; }
}
