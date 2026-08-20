namespace web.Services.Sms.Dtos.Sms;

public sealed class SendSmsResponseDto
{
    public Guid MessageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public int SegmentCount { get; set; }
    public decimal UnitPriceDkk { get; set; }
    public decimal TotalPriceDkk { get; set; }
}