using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaChatMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("thinking")]
    public string? Thinking { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCallDto>? ToolCalls { get; set; }
}
