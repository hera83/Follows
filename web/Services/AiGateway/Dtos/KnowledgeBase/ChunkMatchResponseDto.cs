using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class ChunkMatchResponseDto
{
    [JsonPropertyName("chunkId")]
    public Guid ChunkId { get; set; }

    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; set; }

    [JsonPropertyName("documentFileName")]
    public string? DocumentFileName { get; set; }

    [JsonPropertyName("groupId")]
    public Guid GroupId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}
