namespace web.Constants
{
    /// <summary>
    /// Tuning knobs for the bulk UI-catalog translation pipeline (menu/Feed/Documents/Profil chrome) —
    /// see web/Infrastructure/UiTranslation/*. Separate from the unrelated, already-existing
    /// Documents/Feed/toast translation features (DocumentLimits, FeedLimits, ToastTranslation.cs).
    /// </summary>
    public static class UiTranslationLimits
    {
        /// <summary>How many Danish UI strings are sent to the AI Gateway per LanguageTools.TranslateBatchAsync call.</summary>
        public const int BatchSize = 25;

        /// <summary>
        /// How many known UI strings must be missing for a language before login/profile-save shows the
        /// "siden oversættes, vent venligst" wait page (see UiLocalizationController.Preparing). A small
        /// gap (e.g. a couple of new strings after a deploy) is left to self-heal silently via the
        /// background sweep instead of interrupting every login with a spinner.
        /// </summary>
        public const int GapThreshold = 5;

        /// <summary>
        /// Hard upper bound on one bulk-translation job (see UiLocalizationController.Preparing) — same
        /// reasoning as DocumentLimits.TranslationJobTimeoutMinutes: without this a stuck AI Gateway call
        /// would leave the wait page polling forever.
        /// </summary>
        public const int JobTimeoutMinutes = 15;

        /// <summary>
        /// How often UiTranslationBackgroundWorker drains UiTranslationMissQueue and retries/registers
        /// misses that weren't part of an explicit login/profile-save job — the self-healing path for
        /// strings added after a deploy, or a bulk job that gave up on some batches.
        /// </summary>
        public const int BackgroundSweepIntervalMinutes = 5;
    }
}
