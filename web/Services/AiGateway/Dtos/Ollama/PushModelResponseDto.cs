using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class PushModelResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
