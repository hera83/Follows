namespace web.Services.Sms.Dtos.Sms;

public sealed class GetStatusSmsResponseDto
{
    public Guid MessageId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? From { get; set; }
    public int SegmentCount { get; set; }
    public decimal UnitPriceDkk { get; set; }
    public decimal TotalPriceDkk { get; set; }
    public bool IsBalanceDeducted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public int? ModemReference { get; set; }
    public string? FailureReason { get; set; }
}
