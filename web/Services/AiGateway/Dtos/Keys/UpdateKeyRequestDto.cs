using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Keys;

public class UpdateKeyRequestDto
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("responsibleName")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ResponsibleName { get; set; } = string.Empty;

    [JsonPropertyName("contactInfo")]
    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string ContactInfo { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
