namespace web.Constants
{
    /// <summary>
    /// Supported UI languages a user can choose as their preferred language.
    /// Codes are ISO 639-1. Names are shown in the language's own local form
    /// (autonym) rather than translated to Danish, e.g. Spanish is "Español".
    /// </summary>
    public static class AppLanguages
    {
        /// <summary>Default language code (Danish) — used for new users and as fallback.</summary>
        public const string Default = "da";

        /// <summary>
        /// All supported languages, in display order. Dansk is listed first (the default);
        /// the rest are ordered roughly by number of speakers worldwide.
        /// </summary>
        public static readonly IReadOnlyList<(string Code, string NativeName)> All = new (string, string)[]
        {
            ("da", "Dansk"),
            ("en", "English"),
            ("zh", "中文"),
            ("hi", "हिन्दी"),
            ("es", "Español"),
            ("fr", "Français"),
            ("ar", "العربية"),
            ("bn", "বাংলা"),
            ("pt", "Português"),
            ("ru", "Русский"),
            ("ur", "اردو"),
            ("id", "Bahasa Indonesia"),
            ("de", "Deutsch"),
            ("ja", "日本語"),
            ("mr", "मराठी"),
            ("te", "తెలుగు"),
            ("tr", "Türkçe"),
            ("ta", "தமிழ்"),
            ("vi", "Tiếng Việt"),
            ("ko", "한국어"),
            ("it", "Italiano"),
            ("th", "ไทย"),
            ("fa", "فارسی"),
            ("sw", "Kiswahili"),
            ("pl", "Polski"),
            ("uk", "Українська"),
            ("nl", "Nederlands"),
            ("el", "Ελληνικά"),
            ("sv", "Svenska"),
            ("no", "Norsk"),
            ("fi", "Suomi"),
            ("is", "Íslenska"),
        };

        /// <summary>All valid language codes.</summary>
        public static readonly string[] AllCodes = All.Select(l => l.Code).ToArray();

        /// <summary>Returns true if <paramref name="code"/> is one of the supported language codes.</summary>
        public static bool IsValid(string? code) => !string.IsNullOrEmpty(code) && AllCodes.Contains(code);

        /// <summary>Normalizes a code to a supported value, falling back to <see cref="Default"/>.</summary>
        public static string Normalize(string? code) => IsValid(code) ? code! : Default;

        /// <summary>Native (autonym) display name for a language code, e.g. "da" -&gt; "Dansk". Normalizes first.</summary>
        public static string GetNativeName(string? code) => All.First(l => l.Code == Normalize(code)).NativeName;

        // BCP-47 locale tags for JS Date formatting (toLocaleDateString/toLocaleString) and relative-time
        // phrasing in Feed - see AppLanguages.GetLocaleTag and Feed/Index.cshtml's script block. One
        // reasonable regional variant per language; not meant to cover every regional dialect.
        private static readonly Dictionary<string, string> LocaleTags = new()
        {
            ["da"] = "da-DK",
            ["en"] = "en-US",
            ["zh"] = "zh-CN",
            ["hi"] = "hi-IN",
            ["es"] = "es-ES",
            ["fr"] = "fr-FR",
            ["ar"] = "ar-SA",
            ["bn"] = "bn-BD",
            ["pt"] = "pt-PT",
            ["ru"] = "ru-RU",
            ["ur"] = "ur-PK",
            ["id"] = "id-ID",
            ["de"] = "de-DE",
            ["ja"] = "ja-JP",
            ["mr"] = "mr-IN",
            ["te"] = "te-IN",
            ["tr"] = "tr-TR",
            ["ta"] = "ta-IN",
            ["vi"] = "vi-VN",
            ["ko"] = "ko-KR",
            ["it"] = "it-IT",
            ["th"] = "th-TH",
            ["fa"] = "fa-IR",
            ["sw"] = "sw-KE",
            ["pl"] = "pl-PL",
            ["uk"] = "uk-UA",
            ["nl"] = "nl-NL",
            ["el"] = "el-GR",
            ["sv"] = "sv-SE",
            ["no"] = "nb-NO",
            ["fi"] = "fi-FI",
            ["is"] = "is-IS",
        };

        /// <summary>BCP-47 locale tag for a language code, e.g. "da" -&gt; "da-DK". Normalizes first, falls back to "da-DK".</summary>
        public static string GetLocaleTag(string? code) =>
            LocaleTags.TryGetValue(Normalize(code), out var tag) ? tag : LocaleTags[Default];

        // Maps the Danish language names LanguageTools.DetectLanguageAsync can answer with back to
        // codes above. A few common alternate spellings are included since the model isn't always
        // perfectly consistent (e.g. "Farsi" vs. "Persisk").
        private static readonly Dictionary<string, string> DanishNameToCode = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dansk"] = "da",
            ["Engelsk"] = "en",
            ["Kinesisk"] = "zh", ["Mandarin"] = "zh",
            ["Hindi"] = "hi",
            ["Spansk"] = "es",
            ["Fransk"] = "fr",
            ["Arabisk"] = "ar",
            ["Bengalsk"] = "bn",
            ["Portugisisk"] = "pt",
            ["Russisk"] = "ru",
            ["Urdu"] = "ur",
            ["Indonesisk"] = "id",
            ["Tysk"] = "de",
            ["Japansk"] = "ja",
            ["Marathi"] = "mr",
            ["Telugu"] = "te",
            ["Tyrkisk"] = "tr",
            ["Tamil"] = "ta",
            ["Vietnamesisk"] = "vi",
            ["Koreansk"] = "ko",
            ["Italiensk"] = "it",
            ["Thai"] = "th", ["Thailandsk"] = "th",
            ["Persisk"] = "fa", ["Farsi"] = "fa",
            ["Swahili"] = "sw",
            ["Polsk"] = "pl",
            ["Ukrainsk"] = "uk",
            ["Nederlandsk"] = "nl", ["Hollandsk"] = "nl",
            ["Græsk"] = "el",
            ["Svensk"] = "sv",
            ["Norsk"] = "no",
            ["Finsk"] = "fi",
            ["Islandsk"] = "is"
        };

        /// <summary>
        /// Maps a Danish language name (as returned by LanguageTools.DetectLanguageAsync, e.g. "Engelsk")
        /// back to its code (e.g. "en"). Returns null if the name isn't recognized.
        /// </summary>
        public static string? CodeFromDanishName(string? danishName)
        {
            if (string.IsNullOrWhiteSpace(danishName)) return null;
            return DanishNameToCode.TryGetValue(danishName.Trim(), out var code) ? code : null;
        }
    }
}
