namespace web.Services.AiGateway.Dtos.Speaches;

public class CreateSpeechEmbeddingRequestDto
{
    public string? Model { get; set; }
    public Stream FileContent { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}
