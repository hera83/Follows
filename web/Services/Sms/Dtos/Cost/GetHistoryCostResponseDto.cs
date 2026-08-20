namespace web.Services.Sms.Dtos.Cost;

public sealed class GetHistoryCostResponseDto
{
    public Guid Id { get; set; }
    public decimal OldSmsPriceDkk { get; set; }
    public decimal NewSmsPriceDkk { get; set; }
    public DateTime ChangedAt { get; set; }
}
