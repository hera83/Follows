using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Keys;

public class AuditLogEntryResponseDto
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
