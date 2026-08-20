namespace web.Services.AiGateway.Dtos.Speaches;

public class DiarizeRequestDto
{
    public Stream FileContent { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public List<string>? KnownSpeakerNames { get; set; }
    public List<string>? KnownSpeakerReferences { get; set; }
}
