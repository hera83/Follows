using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class SearchRequestDto
{
    [JsonPropertyName("query")]
    [Required]
    [MinLength(1)]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("topK")]
    [Range(1, 100)]
    public int? TopK { get; set; }
}
