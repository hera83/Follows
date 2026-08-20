using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using web.Services.Sms.Dtos.Cost;
using web.Services.Sms.Dtos.Health;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Dtos.Subscriptions;
using web.Services.Sms.Interfaces;

namespace web.Services.Sms
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _defaultApiKey;
        private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

        public SmsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _defaultApiKey = configuration["Sms:ApiKey"];
        }

        public Task<GetHealthResponseDto?> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            return SendAsync<GetHealthResponseDto>(HttpMethod.Get, "api/Health/Get", null, null, cancellationToken);
        }

        public Task<GetCurrentCostResponseDto?> GetCurrentCostAsync(string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<GetCurrentCostResponseDto>(HttpMethod.Get, "api/Cost/GetCurrent", null, apiKey, cancellationToken);
        }

        public async Task<IReadOnlyList<GetHistoryCostResponseDto>> GetCostHistoryAsync(string? apiKey = null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync<List<GetHistoryCostResponseDto>>(HttpMethod.Get, "api/Cost/GetHistory", null, apiKey, cancellationToken);
            return response ?? [];
        }

        public Task<GetUsageReportCostResponseDto?> GetUsageReportCostAsync(GetUsageReportCostRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<GetUsageReportCostResponseDto>(HttpMethod.Post, "api/Cost/GetUsageReport", request, apiKey, cancellationToken);
        }

        public Task<GetBalanceCostResponseDto?> GetBalanceCostAsync(string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<GetBalanceCostResponseDto>(HttpMethod.Get, "api/Cost/GetBalance", null, apiKey, cancellationToken);
        }

        public Task<SendSmsResponseDto?> SendSmsAsync(SendSmsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<SendSmsResponseDto>(HttpMethod.Post, "api/Sms/Send", request, apiKey, cancellationToken);
        }

        public Task<GetStatusSmsResponseDto?> GetSmsStatusAsync(GetStatusSmsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<GetStatusSmsResponseDto>(HttpMethod.Get, $"api/Sms/Status/{request.MessageId}", null, apiKey, cancellationToken);
        }

        public async Task<IReadOnlyList<ReadSmsResponseDto>> ReadSmsAsync(string? phoneNumber = null, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            var url = string.IsNullOrWhiteSpace(phoneNumber)
                ? "api/Sms/Read"
                : QueryHelpers.AddQueryString("api/Sms/Read", "phoneNumber", phoneNumber);

            var response = await SendAsync<List<ReadSmsResponseDto>>(HttpMethod.Get, url, null, apiKey, cancellationToken);
            return response ?? [];
        }

        public async Task<IReadOnlyList<GetAllSubscriptionsResponseDto>> GetAllSubscriptionsAsync(string? phoneNumber = null, bool? isActive = null, Guid? apiKeyId = null, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(phoneNumber)) parameters["phoneNumber"] = phoneNumber;
            if (isActive.HasValue) parameters["isActive"] = isActive.Value.ToString().ToLowerInvariant();
            if (apiKeyId.HasValue) parameters["apiKeyId"] = apiKeyId.Value.ToString();
            var url = QueryHelpers.AddQueryString("api/Subscriptions/GetAll", parameters);
            var response = await SendAsync<List<GetAllSubscriptionsResponseDto>>(HttpMethod.Get, url, null, apiKey, cancellationToken);
            return response ?? [];
        }

        public Task<GetByIdSubscriptionsResponseDto?> GetSubscriptionByIdAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<GetByIdSubscriptionsResponseDto>(HttpMethod.Get, $"api/Subscriptions/GetById/{id}", null, apiKey, cancellationToken);
        }

        public Task<CreateSubscriptionsResponseDto?> CreateSubscriptionAsync(CreateSubscriptionsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<CreateSubscriptionsResponseDto>(HttpMethod.Post, "api/Subscriptions/Create", request, apiKey, cancellationToken);
        }

        public Task<UpdateSubscriptionsResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionsRequestDto request, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return SendAsync<UpdateSubscriptionsResponseDto>(HttpMethod.Put, $"api/Subscriptions/Update/{id}", request, apiKey, cancellationToken);
        }

        public Task<bool> DeleteSubscriptionAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return DeleteAsync($"api/Subscriptions/Delete/{id}", apiKey, cancellationToken);
        }

        public Task<bool> DeleteSmsAsync(Guid id, string? apiKey = null, CancellationToken cancellationToken = default)
        {
            return DeleteAsync($"api/Sms/Delete/{id}", apiKey, cancellationToken);
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string relativeUrl, object? payload, string? apiKey, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            SetApiKey(request, apiKey);

            if (payload is not null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions), Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content is null
                    ? null
                    : await response.Content.ReadAsStringAsync(cancellationToken);

                throw new HttpRequestException(
                    $"SMS API request failed ({(int)response.StatusCode} {response.StatusCode}) for '{relativeUrl}'. Body: {body}",
                    null,
                    response.StatusCode);
            }

            if (response.Content is null)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions, cancellationToken);
        }

        private async Task<bool> DeleteAsync(string relativeUrl, string? apiKey, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, relativeUrl);
            SetApiKey(request, apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content is null
                    ? null
                    : await response.Content.ReadAsStringAsync(cancellationToken);

                throw new HttpRequestException(
                    $"SMS API delete request failed ({(int)response.StatusCode} {response.StatusCode}) for '{relativeUrl}'. Body: {body}",
                    null,
                    response.StatusCode);
            }

            return true;
        }

        private void SetApiKey(HttpRequestMessage request, string? apiKey)
        {
            var value = string.IsNullOrWhiteSpace(apiKey) ? _defaultApiKey : apiKey;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            request.Headers.Remove("x-api-key");
            request.Headers.TryAddWithoutValidation("x-api-key", value);
        }
    }
}
