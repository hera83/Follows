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
    }
}
