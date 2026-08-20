using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class RagChatRequestDto
{
    [JsonPropertyName("model")]
    [Required]
    [MinLength(1)]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    [Required]
    [MinLength(1)]
    public List<ChatMessageDto> Messages { get; set; } = new();

    [JsonPropertyName("topK")]
    [Range(1, 100)]
    public int? TopK { get; set; }
}
