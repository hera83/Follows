using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Services.AiGateway.Interfaces;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>
    /// Finds and fills the "gap" between the canonical Danish ("da") catalog registry and a target
    /// language's UiTranslationEntry rows — the shared engine behind both the explicit login/profile-save
    /// wait page (UiLocalizationController) and the periodic self-healing sweep
    /// (UiTranslationBackgroundWorker), so there's exactly one place that decides what "needs translating"
    /// means and how batches are built/persisted.
    /// </summary>
    /// <summary>Per-language row for the Settings → Sprog overview table — see <see cref="IUiTranslationBulkService.GetProgressOverviewAsync"/>.</summary>
    public sealed record LanguageTranslationProgress(
        string LanguageCode,
        string NativeName,
        int TranslatedCount,
        int TotalCount,
        int PercentComplete,
        bool IsRunning,
        int RunningCompleted,
        int RunningTotal,
        bool IsInstalled);

    public interface IUiTranslationBulkService
    {
        /// <summary>
        /// Drains UiTranslationMissQueue and registers every distinct missed Danish source string into the
        /// canonical "da" registry (TranslatedText == SourceText), regardless of which viewer's language
        /// triggered the miss — the same "step 1" UiTranslationBackgroundWorker's periodic sweep does.
        /// RunAsync calls this itself before computing a gap, so an explicit trigger (login/profile-save
        /// wait page, Settings → Sprog "Tilføj sprog"/"Opdater") also promotes whatever the viewer's most
        /// recent page view just queued — it doesn't have to wait for the next background sweep (up to
        /// UiTranslationLimits.BackgroundSweepIntervalMinutes later) to see brand-new strings at all.
        /// </summary>
        Task RegisterPendingMissesAsync(CancellationToken cancellationToken);

        /// <summary>Number of known Danish source strings that don't yet have a row for <paramref name="languageCode"/>. Always 0 for "da" itself.</summary>
        Task<int> CountGapAsync(string languageCode, CancellationToken cancellationToken);

        /// <summary>
        /// Translates every currently-missing entry for <paramref name="languageCode"/>, in batches (see
        /// UiTranslationLimits.BatchSize). A batch that fails (bad response, gateway error, mismatched
        /// count) is skipped, not retried inline — those entries simply keep falling back to Danish at
        /// render time (UiTranslationCatalogService.T) until a later sweep succeeds. Never throws out of
        /// the whole run. No-ops immediately if a run for this language is already in flight (see
        /// UiTranslationLanguageStatusTracker) — safe to call from multiple trigger paths concurrently.
        /// Live progress is published to UiTranslationLanguageStatusTracker throughout the run.
        /// </summary>
        Task RunAsync(string languageCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// One row per supported non-Danish language (see AppLanguages.All) for the Settings → Sprog tab:
        /// how many of the known Danish UI strings have a translation for it (count + %), whether a bulk
        /// run is in progress for it right now, and whether it's installed (see InstallLanguageAsync).
        /// </summary>
        Task<IReadOnlyList<LanguageTranslationProgress>> GetProgressOverviewAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Marks <paramref name="languageCode"/> as installed (see InstalledLanguage) — makes it available
        /// on every user's Profile page, regardless of how much of the catalog is translated for it yet.
        /// Idempotent: returns false if it was already installed, true if this call just installed it.
        /// Does NOT itself start translating — callers (SettingsController.AddLanguage) kick RunAsync
        /// separately, since re-installing an already-installed language to top it up shouldn't re-stamp
        /// InstalledAtUtc.
        /// </summary>
        Task<bool> InstallLanguageAsync(string languageCode, CancellationToken cancellationToken);

        /// <summary>
        /// Removes <paramref name="languageCode"/> from the installed set — it disappears from new
        /// Profile dropdowns (existing users already on it keep it, see ProfileController.Index) and from
        /// the Settings → Sprog table. Also deletes every UiTranslationEntry row for that language, so a
        /// later re-install (InstallLanguageAsync + RunAsync) translates the whole catalog again from
        /// scratch rather than reusing the old cached text. Returns false if it wasn't installed.
        /// </summary>
        Task<bool> UninstallLanguageAsync(string languageCode, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes every UiTranslationEntry row for <paramref name="languageCode"/> WITHOUT touching its
        /// InstalledLanguage row — used by the Settings → Sprog "Opdater" button (see
        /// SettingsController.RefreshLanguage) to force a genuinely fresh re-translation of an
        /// already-installed language (e.g. after fixing/adding Danish source text) instead of RunAsync's
        /// normal gap-only top-up, which is a no-op once a language is already at 100%. Returns false if
        /// the language isn't installed. Caller is expected to follow up with RunAsync to actually
        /// re-populate it.
        /// </summary>
        Task<bool> ClearTranslationsAsync(string languageCode, CancellationToken cancellationToken);

        /// <summary>
        /// Danish (always first, always included) plus every explicitly installed language, native-name
        /// sorted — the exact list Profile's language dropdown offers. See ProfileController.Index for how
        /// a user's own already-selected language is kept in the list even if it was later uninstalled.
        /// </summary>
        Task<IReadOnlyList<(string Code, string NativeName)>> GetInstalledLanguagesAsync(CancellationToken cancellationToken);
    }

    public sealed class UiTranslationBulkService : IUiTranslationBulkService
    {
        private readonly ApplicationDbContext _db;
        private readonly LanguageTools _language;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;
        private readonly UiTranslationLanguageStatusTracker _statusTracker;
        private readonly UiTranslationMissQueue _missQueue;
        private readonly ILogger<UiTranslationBulkService> _logger;

        public UiTranslationBulkService(
            ApplicationDbContext db,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
            UiTranslationLanguageStatusTracker statusTracker,
            UiTranslationMissQueue missQueue,
            ILogger<UiTranslationBulkService> logger)
        {
            _db = db;
            _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
            _statusTracker = statusTracker;
            _missQueue = missQueue;
            _logger = logger;
        }

        public async Task RegisterPendingMissesAsync(CancellationToken cancellationToken)
        {
            var misses = _missQueue.DrainAll();
            if (misses.Count == 0) return;

            var distinctSources = misses
                .GroupBy(m => m.Key.Hash)
                .Select(g => new { Hash = g.Key, Text = g.First().Value })
                .ToList();

            var existingDaHashes = (await _db.UiTranslationEntries
                .Where(e => e.LanguageCode == AppLanguages.Default)
                .Select(e => e.SourceTextHash)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var now = DateTime.UtcNow;
            foreach (var source in distinctSources)
            {
                if (existingDaHashes.Contains(source.Hash)) continue;

                _db.UiTranslationEntries.Add(new UiTranslationEntry
                {
                    SourceTextHash = source.Hash,
                    SourceText = source.Text,
                    LanguageCode = AppLanguages.Default,
                    TranslatedText = source.Text,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CountGapAsync(string languageCode, CancellationToken cancellationToken)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            if (languageCode == AppLanguages.Default) return 0;

            var missing = await GetMissingEntriesAsync(languageCode, cancellationToken);
            return missing.Count;
        }

        public async Task RunAsync(string languageCode, CancellationToken cancellationToken = default)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            if (languageCode == AppLanguages.Default) return;

            // Global dedup across all three trigger paths (login/profile-save wait page, background
            // sweep, admin "Tilføj sprog") - see UiTranslationLanguageStatusTracker. If a run for this
            // language is already in flight, whoever started it will finish the job; this call is a no-op.
            if (!_statusTracker.TryStart(languageCode)) return;

            try
            {
                // Promote whatever's still sitting in the miss queue (e.g. the page view that just
                // triggered this run) into the "da" registry first - without this, a brand-new string
                // that was never seen by a background sweep yet would be invisible to the gap check below
                // and this run would silently do nothing for it (see RegisterPendingMissesAsync).
                await RegisterPendingMissesAsync(cancellationToken);

                var missing = await GetMissingEntriesAsync(languageCode, cancellationToken);
                _statusTracker.ReportProgress(languageCode, 0, missing.Count);
                if (missing.Count == 0) return;

                var targetNative = AppLanguages.GetNativeName(languageCode);
                var aiConfig = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
                var model = string.IsNullOrWhiteSpace(aiConfig.TranslationModel) ? null : aiConfig.TranslationModel;

                var completed = 0;
                foreach (var batch in Chunk(missing, UiTranslationLimits.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await TranslateBatchAsync(batch, languageCode, targetNative, model, cancellationToken);

                    completed += batch.Count;
                    _statusTracker.ReportProgress(languageCode, completed, missing.Count);
                }
            }
            finally
            {
                // Always marked not-running, even on a timeout/cancellation/unexpected exception - a
                // caller polling UiTranslationLanguageStatusTracker (the wait page, Settings → Sprog) must
                // never see "running" get stuck forever. Whatever didn't finish stays on Danish fallback
                // and is picked up by the next sweep/trigger.
                _statusTracker.Complete(languageCode);
            }
        }

        public async Task<IReadOnlyList<LanguageTranslationProgress>> GetProgressOverviewAsync(CancellationToken cancellationToken)
        {
            var totalKnown = await _db.UiTranslationEntries
                .AsNoTracking()
                .CountAsync(e => e.LanguageCode == AppLanguages.Default, cancellationToken);

            var translatedCounts = await _db.UiTranslationEntries
                .AsNoTracking()
                .Where(e => e.LanguageCode != AppLanguages.Default)
                .GroupBy(e => e.LanguageCode)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Language, x => x.Count, cancellationToken);

            var installedCodes = (await _db.InstalledLanguages
                .AsNoTracking()
                .Select(i => i.LanguageCode)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var result = new List<LanguageTranslationProgress>();
            foreach (var (code, nativeName) in AppLanguages.All)
            {
                if (code == AppLanguages.Default) continue;

                var translated = translatedCounts.GetValueOrDefault(code);
                var percent = totalKnown == 0 ? 0 : (int)Math.Round(translated * 100.0 / totalKnown);
                var status = _statusTracker.Get(code);

                result.Add(new LanguageTranslationProgress(
                    code, nativeName, translated, totalKnown, percent,
                    status?.IsRunning ?? false, status?.Completed ?? 0, status?.Total ?? 0,
                    installedCodes.Contains(code)));
            }

            return result;
        }

        public async Task<bool> InstallLanguageAsync(string languageCode, CancellationToken cancellationToken)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            if (languageCode == AppLanguages.Default) return false; // already always available, never stored

            var exists = await _db.InstalledLanguages.AnyAsync(i => i.LanguageCode == languageCode, cancellationToken);
            if (exists) return false;

            _db.InstalledLanguages.Add(new InstalledLanguage { LanguageCode = languageCode, InstalledAtUtc = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UninstallLanguageAsync(string languageCode, CancellationToken cancellationToken)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            if (languageCode == AppLanguages.Default) return false; // Danish can't be uninstalled

            var existing = await _db.InstalledLanguages.FirstOrDefaultAsync(i => i.LanguageCode == languageCode, cancellationToken);
            if (existing is null) return false;

            _db.InstalledLanguages.Remove(existing);

            // Deletes the translated cache too, not just the "installed" flag - re-installing this
            // language later starts a genuinely fresh translation pass (see InstallLanguageAsync/RunAsync),
            // not a reuse of whatever was cached before it was removed.
            await _db.UiTranslationEntries
                .Where(e => e.LanguageCode == languageCode)
                .ExecuteDeleteAsync(cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ClearTranslationsAsync(string languageCode, CancellationToken cancellationToken)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            if (languageCode == AppLanguages.Default) return false; // Danish is the canonical registry, never cleared

            var isInstalled = await _db.InstalledLanguages.AnyAsync(i => i.LanguageCode == languageCode, cancellationToken);
            if (!isInstalled) return false;

            await _db.UiTranslationEntries
                .Where(e => e.LanguageCode == languageCode)
                .ExecuteDeleteAsync(cancellationToken);

            return true;
        }

        public async Task<IReadOnlyList<(string Code, string NativeName)>> GetInstalledLanguagesAsync(CancellationToken cancellationToken)
        {
            var installedCodes = await _db.InstalledLanguages
                .AsNoTracking()
                .Select(i => i.LanguageCode)
                .ToListAsync(cancellationToken);
            var installedSet = installedCodes.ToHashSet();

            var result = new List<(string Code, string NativeName)>
            {
                (AppLanguages.Default, AppLanguages.GetNativeName(AppLanguages.Default))
            };

            result.AddRange(AppLanguages.All
                .Where(l => l.Code != AppLanguages.Default && installedSet.Contains(l.Code))
                .OrderBy(l => l.NativeName)
                .Select(l => (l.Code, l.NativeName)));

            return result;
        }

        private async Task TranslateBatchAsync(
            List<(string Hash, string SourceText)> batch,
            string languageCode,
            string targetNative,
            string? model,
            CancellationToken cancellationToken)
        {
            try
            {
                var texts = batch.Select(b => b.SourceText).ToList();
                var translated = await _language.TranslateBatchAsync(texts, targetNative, "Dansk", model, cancellationToken);

                if (translated is null || translated.Count != texts.Count)
                {
                    _logger.LogWarning(
                        "UI catalog batch translate to {Language} returned {Count} item(s) for {Expected} input(s) — skipping this batch, entries stay on Danish fallback until a later retry",
                        languageCode, translated?.Count ?? -1, texts.Count);
                    return;
                }

                var now = DateTime.UtcNow;
                for (var i = 0; i < batch.Count; i++)
                {
                    var text = translated[i];
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    _db.UiTranslationEntries.Add(new UiTranslationEntry
                    {
                        SourceTextHash = batch[i].Hash,
                        SourceText = batch[i].SourceText,
                        LanguageCode = languageCode,
                        TranslatedText = text,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Never let one bad batch (gateway hiccup, a unique-index race with a concurrent sweep,
                // ...) take down the whole run — the entries in it simply stay on Danish fallback (see
                // UiTranslationCatalogService.T) until UiTranslationBackgroundWorker retries them.
                _logger.LogWarning(ex,
                    "UI catalog batch translate to {Language} failed — {Count} string(s) left on Danish fallback for now",
                    languageCode, batch.Count);
            }
        }

        /// <summary>Danish source strings ("da" rows) that don't yet have a row for <paramref name="languageCode"/>.</summary>
        private async Task<List<(string Hash, string SourceText)>> GetMissingEntriesAsync(string languageCode, CancellationToken cancellationToken)
        {
            var daEntries = await _db.UiTranslationEntries
                .AsNoTracking()
                .Where(e => e.LanguageCode == AppLanguages.Default)
                .Select(e => new { e.SourceTextHash, e.SourceText })
                .ToListAsync(cancellationToken);
            if (daEntries.Count == 0) return [];

            var existingHashes = await _db.UiTranslationEntries
                .AsNoTracking()
                .Where(e => e.LanguageCode == languageCode)
                .Select(e => e.SourceTextHash)
                .ToListAsync(cancellationToken);
            var existingSet = existingHashes.ToHashSet();

            return daEntries
                .Where(e => !existingSet.Contains(e.SourceTextHash))
                .Select(e => (e.SourceTextHash, e.SourceText))
                .ToList();
        }

        private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
                yield return source.GetRange(i, Math.Min(size, source.Count - i));
        }
    }
}
