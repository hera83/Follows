namespace web.Services.Sms.Dtos.Cost;

public sealed class GetUsageReportCostRequestDto
{
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? Status { get; set; }
    public string? ToPhoneNumber { get; set; }
    public string? MessageContains { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}