namespace web.Services.AiGateway.Dtos.Speaches;

public class TranscribeRequestDto
{
    public string? Model { get; set; }
    public Stream FileContent { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Language { get; set; }
    public string? Prompt { get; set; }
    public double? Temperature { get; set; }
    public List<string>? TimestampGranularities { get; set; }
    public string? Hotwords { get; set; }
    public bool? WithoutTimestamps { get; set; }
}
