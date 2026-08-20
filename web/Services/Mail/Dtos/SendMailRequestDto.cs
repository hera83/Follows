namespace web.Services.Mail.Dtos;

public sealed class SendMailRequestDto
{
    public List<string> To { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public string? ReplyTo { get; set; }
    public List<MailAttachmentDto> Attachments { get; set; } = [];
}
