using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class ChatRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("messages")]
    public List<OllamaMessageDto>? Messages { get; set; }

    [JsonPropertyName("tools")]
    public List<OllamaToolDto>? Tools { get; set; }

    [JsonPropertyName("format")]
    public JsonElement? Format { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptionsDto? Options { get; set; }

    [JsonPropertyName("keepAlive")]
    public string? KeepAlive { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}
