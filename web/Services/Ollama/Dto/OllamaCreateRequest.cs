using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaCreateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("license")]
    public JsonElement? License { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, JsonElement>? Parameters { get; set; }

    [JsonPropertyName("messages")]
    public List<OllamaChatMessageDto>? Messages { get; set; }

    [JsonPropertyName("quantize")]
    public string? Quantize { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
}
