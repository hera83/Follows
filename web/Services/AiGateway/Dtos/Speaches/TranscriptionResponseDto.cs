using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class TranscriptionResponseDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("segments")]
    public List<TranscriptionSegmentDto>? Segments { get; set; }

    [JsonPropertyName("words")]
    public List<TranscriptionWordDto>? Words { get; set; }
}
