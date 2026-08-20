namespace web.Services.Mail.Dtos;

public sealed class SendMailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
