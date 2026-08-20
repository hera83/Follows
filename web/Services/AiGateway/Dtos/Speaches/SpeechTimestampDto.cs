using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class SpeechTimestampDto
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}
