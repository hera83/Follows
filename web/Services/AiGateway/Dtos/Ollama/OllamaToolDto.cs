using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaToolDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public OllamaToolFunctionDto? Function { get; set; }
}
