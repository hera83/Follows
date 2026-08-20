using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class TranscriptionWordDto
{
    [JsonPropertyName("word")]
    public string? Word { get; set; }

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }
}
