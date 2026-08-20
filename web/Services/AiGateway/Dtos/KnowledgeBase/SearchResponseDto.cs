using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class SearchResponseDto
{
    [JsonPropertyName("matches")]
    public List<ChunkMatchResponseDto>? Matches { get; set; }
}
