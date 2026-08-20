using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class SynthesizeRequestDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    [JsonPropertyName("responseFormat")]
    public string? ResponseFormat { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    [JsonPropertyName("sampleRate")]
    public int? SampleRate { get; set; }
}
