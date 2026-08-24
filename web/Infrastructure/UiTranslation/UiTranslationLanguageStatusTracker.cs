using System.Collections.Concurrent;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>Live state for one language's UI-catalog bulk translation — see <see cref="UiTranslationLanguageStatusTracker"/>.</summary>
    public sealed class UiTranslationLanguageStatus
    {
        public bool IsRunning { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
        public DateTime? LastRunStartedUtc { get; set; }
        public DateTime? LastRunCompletedUtc { get; set; }
    }

    /// <summary>
    /// Singleton, in-process, per-language status for the bulk UI-catalog translation pipeline —
    /// replaces the earlier per-user UiLocalizationJobTracker. Keyed purely by language code (not by who
    /// triggered it), because "is French being translated right now, and how far along" is the same answer
    /// no matter which of the three trigger paths asked for it:
    ///  - a user logging in / changing their profile language (UiLocalizationController.Preparing),
    ///  - the periodic self-healing sweep (UiTranslationBackgroundWorker),
    ///  - an admin explicitly warming up a language from Settings → Sprog (SettingsController.AddLanguage).
    ///
    /// UiTranslationBulkService.RunAsync calls TryStart before doing any work and returns immediately
    /// (without duplicating an AI Gateway call) if a run for that language is already in flight — this is
    /// what makes the three trigger paths safe to call concurrently without racing each other, and is also
    /// exactly the data the Settings → Sprog tab needs to show live progress regardless of what kicked the
    /// job off.
    /// </summary>
    public sealed class UiTranslationLanguageStatusTracker
    {
        private readonly ConcurrentDictionary<string, UiTranslationLanguageStatus> _status = new();

        /// <summary>Marks <paramref name="languageCode"/> as running, unless it already is — returns false in that case so the caller can skip duplicate work.</summary>
        public bool TryStart(string languageCode)
        {
            var status = _status.GetOrAdd(languageCode, _ => new UiTranslationLanguageStatus());
            lock (status)
            {
                if (status.IsRunning) return false;

                status.IsRunning = true;
                status.Completed = 0;
                status.Total = 0;
                status.LastRunStartedUtc = DateTime.UtcNow;
                return true;
            }
        }

        public void ReportProgress(string languageCode, int completed, int total)
        {
            if (!_status.TryGetValue(languageCode, out var status)) return;
            lock (status)
            {
                status.Completed = completed;
                status.Total = total;
            }
        }

        public void Complete(string languageCode)
        {
            if (!_status.TryGetValue(languageCode, out var status)) return;
            lock (status)
            {
                status.IsRunning = false;
                status.LastRunCompletedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>Snapshot of the current status, or null if this language has never had a run.</summary>
        public UiTranslationLanguageStatus? Get(string languageCode) =>
            _status.TryGetValue(languageCode, out var status) ? status : null;
    }
}
