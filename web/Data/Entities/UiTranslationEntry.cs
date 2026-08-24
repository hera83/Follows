namespace web.Data.Entities
{
    /// <summary>
    /// One (Danish source string, language) → translated string mapping in the UI-wide translation
    /// catalog that backs @T()/@TJs() in Razor views — see web/Infrastructure/UiTranslation/*. This is a
    /// different, additional mechanism from the existing Documents/Feed content translation and
    /// ToastTranslation.cs (on-demand, IMemoryCache-only, never persisted) — those keep working unchanged.
    ///
    /// Rows with LanguageCode == AppLanguages.Default ("da") double as the canonical registry of every
    /// known source string in the app (TranslatedText == SourceText for those) — see
    /// UiTranslationCatalogService/UiTranslationBulkService for how the registry is built and used to
    /// compute translation "gaps" for other languages.
    /// </summary>
    public class UiTranslationEntry
    {
        public int Id { get; set; }

        /// <summary>SHA-256 hex of SourceText — see UiTranslationHasher. Acts as the lookup key, so the Danish text itself never has to be duplicated as a separate hand-picked key name.</summary>
        public string SourceTextHash { get; set; } = string.Empty;

        public string SourceText { get; set; } = string.Empty;

        /// <summary>AppLanguages code, e.g. "da", "en", "fr".</summary>
        public string LanguageCode { get; set; } = string.Empty;

        public string TranslatedText { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
