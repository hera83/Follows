using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Health;

public class HealthCheckEntryDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
