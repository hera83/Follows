using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class ShowModelRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
