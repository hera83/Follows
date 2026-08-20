namespace web.Services.Sms.Dtos.Cost;

public sealed class GetUsageReportCostResponseDto
{
    public Guid? ApiKeyId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalUsageDkk { get; set; }
    public List<GetUsageReportCostItemResponseDto> Items { get; set; } = [];
}

public sealed class GetUsageReportCostItemResponseDto
{
    public Guid SmsId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ApiKeyName { get; set; }
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public decimal UnitPriceDkk { get; set; }
    public decimal TotalPriceDkk { get; set; }
    public bool IsBalanceDeducted { get; set; }
}