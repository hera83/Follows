namespace web.Services.AiGateway.Dtos.KnowledgeBase;

public class UploadDocumentRequestDto
{
    public Guid GroupId { get; set; }
    public Stream FileContent { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}
