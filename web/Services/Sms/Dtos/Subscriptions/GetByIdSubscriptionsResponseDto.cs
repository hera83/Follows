namespace web.Services.Sms.Dtos.Subscriptions;

public sealed class GetByIdSubscriptionsResponseDto
{
    public Guid Id { get; set; }
    public Guid ApiKeyId { get; set; }
    public List<string> PhoneNumbers { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
