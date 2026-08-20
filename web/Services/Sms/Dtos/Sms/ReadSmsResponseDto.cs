namespace web.Services.Sms.Dtos.Sms;

public sealed class ReadSmsResponseDto
{
    public Guid Id { get; init; }
    public string? FromPhoneNumber { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTime? ReceivedAt { get; init; }
    public string Status { get; init; } = string.Empty;
}
