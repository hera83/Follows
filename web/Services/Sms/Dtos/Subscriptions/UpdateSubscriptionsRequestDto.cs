using System.ComponentModel.DataAnnotations;

namespace web.Services.Sms.Dtos.Subscriptions;

public sealed class UpdateSubscriptionsRequestDto
{
    [Required]
    public List<string> PhoneNumbers { get; set; } = new();

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public string? WebhookUrl { get; set; }
}
