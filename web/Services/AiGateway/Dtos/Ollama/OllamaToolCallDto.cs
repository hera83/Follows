using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaToolCallDto
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunctionDto? Function { get; set; }
}
