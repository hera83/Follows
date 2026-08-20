using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Keys;

public class AuditLogResponseDto
{
    [JsonPropertyName("entries")]
    public List<AuditLogEntryResponseDto>? Entries { get; set; }
}
