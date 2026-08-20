using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
