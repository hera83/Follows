using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class ListGroupsResponseDto
{
    [JsonPropertyName("groups")]
    public List<GroupResponseDto>? Groups { get; set; }
}
