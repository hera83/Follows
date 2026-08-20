using web.Services.Sms.Dtos.Cost;
using web.Services.Sms.Dtos.Health;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Dtos.Subscriptions;

namespace web.Services.Sms.Interfaces
{
    public interface ISmsService
    {
        Task<GetHealthResponseDto?> GetHealthAsync(CancellationToken cancellationToken = default);

        Task<GetCurrentCostResponseDto?> GetCurrentCostAsync(string? apiKey = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GetHistoryCostResponseDto>> GetCostHistoryAsync(string? apiKey = null, CancellationToken cancellationToken = default);
        Task<GetUsageReportCostResponseDto?> GetUsageReportCostAsync(GetUsageReportCostRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<GetBalanceCostResponseDto?> GetBalanceCostAsync(string? apiKey = null, CancellationToken cancellationToken = default);

        Task<SendSmsResponseDto?> SendSmsAsync(SendSmsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<GetStatusSmsResponseDto?> GetSmsStatusAsync(GetStatusSmsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReadSmsResponseDto>> ReadSmsAsync(string? phoneNumber = null, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GetAllSubscriptionsResponseDto>> GetAllSubscriptionsAsync(string? phoneNumber = null, bool? isActive = null, Guid? apiKeyId = null, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<GetByIdSubscriptionsResponseDto?> GetSubscriptionByIdAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<CreateSubscriptionsResponseDto?> CreateSubscriptionAsync(CreateSubscriptionsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<UpdateSubscriptionsResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteSubscriptionAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteSmsAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default);
    }
}
