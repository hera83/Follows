using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelSummaryDto> Models { get; set; } = new();
}
