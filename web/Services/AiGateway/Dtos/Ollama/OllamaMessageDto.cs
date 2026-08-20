using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaMessageDto
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("toolCalls")]
    public List<OllamaToolCallDto>? ToolCalls { get; set; }
}
