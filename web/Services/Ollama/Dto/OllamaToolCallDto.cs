using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaToolCallDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public OllamaToolCallFunctionDto? Function { get; set; }
}

public class OllamaToolCallFunctionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; set; }
}
