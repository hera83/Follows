using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
