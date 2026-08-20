using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class DiarizationSegmentDto
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }
}
