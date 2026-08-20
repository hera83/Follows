using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Keys;

public class KeyResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("responsibleName")]
    public string? ResponsibleName { get; set; }

    [JsonPropertyName("contactInfo")]
    public string? ContactInfo { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastRotatedAt")]
    public DateTime? LastRotatedAt { get; set; }

    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }
}
