using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaModelDetailsDto
{
    [JsonPropertyName("parentModel")]
    public string? ParentModel { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("families")]
    public List<string>? Families { get; set; }

    [JsonPropertyName("parameterSize")]
    public string? ParameterSize { get; set; }

    [JsonPropertyName("quantizationLevel")]
    public string? QuantizationLevel { get; set; }
}
