using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class RunningModelsResponseDto
{
    [JsonPropertyName("models")]
    public List<string>? Models { get; set; }
}
