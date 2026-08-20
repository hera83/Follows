using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class CreateModelResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
