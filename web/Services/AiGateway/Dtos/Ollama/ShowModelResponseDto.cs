using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class ShowModelResponseDto
{
    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("modelfile")]
    public string? Modelfile { get; set; }

    [JsonPropertyName("parameters")]
    public string? Parameters { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetailsDto? Details { get; set; }

    [JsonPropertyName("modelInfo")]
    public Dictionary<string, object?>? ModelInfo { get; set; }

    [JsonPropertyName("capabilities")]
    public List<string>? Capabilities { get; set; }
}
