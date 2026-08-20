using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Speaches;

public class TranscriptionSegmentDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("tokens")]
    public List<int>? Tokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("avgLogprob")]
    public double AvgLogprob { get; set; }

    [JsonPropertyName("compressionRatio")]
    public double CompressionRatio { get; set; }

    [JsonPropertyName("noSpeechProb")]
    public double NoSpeechProb { get; set; }

    [JsonPropertyName("seek")]
    public int Seek { get; set; }
}
