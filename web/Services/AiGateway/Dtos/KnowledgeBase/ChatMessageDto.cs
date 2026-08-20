using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class ChatMessageDto
{
    [JsonPropertyName("role")]
    [Required]
    [MinLength(1)]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    [Required]
    [MinLength(1)]
    public string Content { get; set; } = string.Empty;
}
