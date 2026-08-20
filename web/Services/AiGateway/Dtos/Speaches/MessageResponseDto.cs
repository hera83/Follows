using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class MessageResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
