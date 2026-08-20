using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessageDto> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<OllamaToolDto>? Tools { get; set; }

    [JsonPropertyName("format")]
    public JsonElement? Format { get; set; }

    [JsonPropertyName("options")]
    public OllamaRuntimeOptionsDto? Options { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    [JsonPropertyName("think")]
    public JsonElement? Think { get; set; }

    [JsonPropertyName("keep_alive")]
    public JsonElement? KeepAlive { get; set; }

    [JsonPropertyName("logprobs")]
    public bool? LogProbs { get; set; }

    [JsonPropertyName("top_logprobs")]
    public int? TopLogProbs { get; set; }
}
