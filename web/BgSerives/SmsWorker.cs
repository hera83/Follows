using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Interfaces;

namespace web.BgSerives
{
    /// <summary>
    /// Sends queued outbound SMS messages and pulls new inbound SMS messages via <see cref="ISmsService"/>,
    /// keeping the SmsMessages table in sync with the SMS gateway.
    /// </summary>
    public class SmsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SmsWorker> _logger;
        private readonly TimeSpan _interval;

        public SmsWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<SmsWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var intervalSeconds = configuration.GetValue<int?>("Sms:WorkerIntervalSeconds") ?? 30;
            _interval = TimeSpan.FromSeconds(intervalSeconds > 0 ? intervalSeconds : 30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (true)
            {
                try
                {
                    await SendOutboundMessagesAsync(stoppingToken);
                    await PollOutboundStatusesAsync(stoppingToken);
                    await FetchInboundMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SmsWorker iteration failed");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task SendOutboundMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var pending = await dbContext.SmsMessages
                .Where(m => m.Direction == SmsDirection.Outbound && m.GatewayMessageId == null)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
            {
                return;
            }

            foreach (var message in pending)
            {
                try
                {
                    var response = await smsService.SendSmsAsync(
                        new SendSmsRequestDto { To = message.PhoneNumber, Message = message.Body },
                        cancellationToken: cancellationToken);

                    if (response is not null)
                    {
                        message.GatewayMessageId = response.MessageId;
                        message.Status = response.Status;
                        message.SegmentCount = response.SegmentCount;
                        message.UnitPriceDkk = response.UnitPriceDkk;
                        message.TotalPriceDkk = response.TotalPriceDkk;
                        message.SentAtUtc = DateTime.UtcNow;
                    }

                    message.UpdatedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SMS {SmsMessageId} to {PhoneNumber}", message.Id, message.PhoneNumber);
                    message.Status = SmsMessageStatus.Failed;
                    message.FailureReason = ex.Message;
                    message.FailedAtUtc = DateTime.UtcNow;
                    message.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Submitting a message only tells us the gateway accepted it (typically status "Queued");
        /// it does not tell us when the gateway has actually sent it. Without this, a message's
        /// Status would stay frozen at whatever it was at submission time forever, since
        /// <see cref="SendOutboundMessagesAsync"/> only looks at messages without a GatewayMessageId.
        /// This re-checks messages the gateway hasn't reported as final yet and refreshes their Status.
        /// </summary>
        private async Task PollOutboundStatusesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var cutoff = DateTime.UtcNow.AddDays(-7);
            var inFlight = await dbContext.SmsMessages
                .Where(m => m.Direction == SmsDirection.Outbound
                    && m.GatewayMessageId != null
                    && m.FailedAtUtc == null
                    && m.Status != SmsMessageStatus.Sent
                    && m.CreatedAtUtc >= cutoff)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (inFlight.Count == 0)
            {
                return;
            }

            foreach (var message in inFlight)
            {
                try
                {
                    var status = await smsService.GetSmsStatusAsync(
                        new GetStatusSmsRequestDto { MessageId = message.GatewayMessageId!.Value },
                        cancellationToken: cancellationToken);

                    if (status is null)
                    {
                        continue;
                    }

                    message.Status = status.Status;
                    message.SegmentCount = status.SegmentCount;
                    message.UnitPriceDkk = status.UnitPriceDkk;
                    message.TotalPriceDkk = status.TotalPriceDkk;

                    if (status.SentAt.HasValue)
                    {
                        message.SentAtUtc = status.SentAt;
                    }

                    if (status.FailedAt.HasValue)
                    {
                        message.FailedAtUtc = status.FailedAt;
                        message.FailureReason = status.FailureReason;
                    }

                    message.UpdatedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to poll SMS status for {SmsMessageId} (gateway id {GatewayMessageId})", message.Id, message.GatewayMessageId);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task FetchInboundMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var incoming = await smsService.ReadSmsAsync(cancellationToken: cancellationToken);
            if (incoming.Count == 0)
            {
                return;
            }

            var incomingIds = incoming.Select(m => m.Id).ToList();
            var existingIds = await dbContext.SmsMessages
                .Where(m => m.Direction == SmsDirection.Inbound && m.GatewayMessageId != null && incomingIds.Contains(m.GatewayMessageId.Value))
                .Select(m => m.GatewayMessageId!.Value)
                .ToListAsync(cancellationToken);

            var existingIdSet = existingIds.ToHashSet();
            var newMessages = incoming
                .Where(m => !existingIdSet.Contains(m.Id))
                .Select(m => new SmsMessage
                {
                    GatewayMessageId = m.Id,
                    Direction = SmsDirection.Inbound,
                    PhoneNumber = m.FromPhoneNumber ?? string.Empty,
                    Body = m.Body,
                    Status = m.Status,
                    ReceivedAtUtc = m.ReceivedAt,
                    CreatedAtUtc = DateTime.UtcNow,
                })
                .ToList();

            if (newMessages.Count == 0)
            {
                return;
            }

            dbContext.SmsMessages.AddRange(newMessages);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registered {Count} new inbound SMS messages", newMessages.Count);
        }
    }
}
