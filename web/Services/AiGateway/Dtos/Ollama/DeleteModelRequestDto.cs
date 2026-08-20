using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class DeleteModelRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
