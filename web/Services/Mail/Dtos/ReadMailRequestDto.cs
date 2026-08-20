namespace web.Services.Mail.Dtos;

public sealed class ReadMailRequestDto
{
    public string Folder { get; set; } = "INBOX";
    public int MaxCount { get; set; } = 50;
    public bool UnseenOnly { get; set; }
    public bool MarkAsSeen { get; set; }
}
