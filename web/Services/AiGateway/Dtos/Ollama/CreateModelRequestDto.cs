using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class CreateModelRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("files")]
    public Dictionary<string, string>? Files { get; set; }

    [JsonPropertyName("adapters")]
    public Dictionary<string, string>? Adapters { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object?>? Parameters { get; set; }

    [JsonPropertyName("messages")]
    public List<OllamaMessageDto>? Messages { get; set; }

    [JsonPropertyName("quantize")]
    public string? Quantize { get; set; }
}
