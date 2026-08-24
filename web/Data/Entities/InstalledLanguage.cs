namespace web.Data.Entities
{
    /// <summary>
    /// Marks a language as explicitly "installed" — available for any user to pick on their Profile page.
    /// Danish is always available and never has a row here (see AppLanguages.Default handling in
    /// UiTranslationBulkService.GetInstalledLanguagesAsync/ProfileController) — every other language only
    /// shows up on Profile once an admin has added it from Settings → Sprog (see
    /// SettingsController.AddLanguage), regardless of how much of the UI catalog is actually translated
    /// for it yet (that's tracked separately, see UiTranslationEntry).
    /// </summary>
    public class InstalledLanguage
    {
        /// <summary>AppLanguages code, e.g. "en", "fr". Primary key — a language is either installed or it isn't.</summary>
        public string LanguageCode { get; set; } = string.Empty;

        public DateTime InstalledAtUtc { get; set; }
    }
}
