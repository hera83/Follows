using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class EmbedRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("input")]
    public List<string>? Input { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptionsDto? Options { get; set; }

    [JsonPropertyName("keepAlive")]
    public string? KeepAlive { get; set; }
}
