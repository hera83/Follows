namespace web.Services.Sms.Dtos.Cost;

public sealed class GetBalanceCostResponseDto
{
    public Guid ApiKeyId { get; set; }
    public decimal Balance { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
