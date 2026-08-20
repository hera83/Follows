using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Keys;

public class ListKeysResponseDto
{
    [JsonPropertyName("keys")]
    public List<KeyResponseDto>? Keys { get; set; }
}
