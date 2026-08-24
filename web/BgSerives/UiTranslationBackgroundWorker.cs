using web.Constants;
using web.Infrastructure.UiTranslation;

namespace web.BgSerives
{
    /// <summary>
    /// Periodically checks UiTranslationMissQueue for languages with a pending miss and gives each one a
    /// UiTranslationBulkService.RunAsync pass — the self-healing half of the bulk UI-catalog translation
    /// pipeline (the other half is the explicit login/profile-save wait page and Settings → Sprog's
    /// "Tilføj sprog"/"Opdater", see UiLocalizationController/SettingsController). RunAsync itself takes
    /// care of registering any pending misses into the canonical "da" registry before computing its gap
    /// (see UiTranslationBulkService.RegisterPendingMissesAsync) — this worker is just the timer that makes
    /// sure that happens periodically even when nobody explicitly triggers a run. Runs in its own DI scope
    /// each sweep, same reasoning as DocumentTranslationJobTracker's background task — a BackgroundService's
    /// own scope lives for the app's lifetime, so scoped services (DbContext) must be resolved fresh each
    /// time, not held across sweeps.
    /// </summary>
    public sealed class UiTranslationBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UiTranslationMissQueue _missQueue;
        private readonly ILogger<UiTranslationBackgroundWorker> _logger;

        public UiTranslationBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            UiTranslationMissQueue missQueue,
            ILogger<UiTranslationBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _missQueue = missQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(UiTranslationLimits.BackgroundSweepIntervalMinutes);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                    await SweepAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Normal shutdown.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "UI catalog background sweep failed — will retry on the next interval");
                }
            }
        }

        private async Task SweepAsync(CancellationToken cancellationToken)
        {
            // Peek which languages currently have a pending miss before RunAsync (below) drains the
            // queue as a side effect of registering step 1's "da" entries - RunAsync itself also calls
            // UiTranslationBulkService.RegisterPendingMissesAsync (see there), so this sweep doesn't
            // duplicate that logic, it just needs to know which languages to top up afterwards.
            var languagesToTopUp = _missQueue.Peek()
                .Where(l => l != AppLanguages.Default)
                .Distinct()
                .ToList();
            if (languagesToTopUp.Count == 0) return;

            using var scope = _scopeFactory.CreateScope();
            var bulk = scope.ServiceProvider.GetRequiredService<IUiTranslationBulkService>();

            // RunAsync registers any still-pending misses into the "da" registry itself before computing
            // its gap (see UiTranslationBulkService.RunAsync), so the first RunAsync call below also
            // covers step 1 for every language in this sweep, not just the one it's topping up.
            foreach (var language in languagesToTopUp)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await bulk.RunAsync(language, cancellationToken: cancellationToken);
            }
        }
    }
}
