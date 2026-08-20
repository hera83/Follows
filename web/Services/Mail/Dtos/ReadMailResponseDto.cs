namespace web.Services.Mail.Dtos;

public sealed class ReadMailResponseDto
{
    public uint Uid { get; init; }
    public string? MessageId { get; init; }
    public string From { get; init; } = string.Empty;
    public List<string> To { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public bool IsSeen { get; init; }
    public List<MailAttachmentDto> Attachments { get; init; } = [];
}
