using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Health;

public class HealthResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("checks")]
    public List<HealthCheckEntryDto>? Checks { get; set; }
}
