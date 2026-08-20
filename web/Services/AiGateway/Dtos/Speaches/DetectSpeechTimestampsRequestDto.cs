namespace web.Services.AiGateway.Dtos.Speaches;

public class DetectSpeechTimestampsRequestDto
{
    public Stream FileContent { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Model { get; set; }
    public double? Threshold { get; set; }
    public double? NegThreshold { get; set; }
    public int? MinSpeechDurationMs { get; set; }
    public double? MaxSpeechDurationS { get; set; }
    public int? MinSilenceDurationMs { get; set; }
    public int? SpeechPadMs { get; set; }
}
