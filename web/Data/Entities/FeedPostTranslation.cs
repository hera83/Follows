namespace web.Data.Entities
{
    /// <summary>
    /// A cached machine translation of a <see cref="FeedPost"/> caption into one language, so the
    /// same post is only ever sent through the translation service once per language, not on every view.
    /// </summary>
    public class FeedPostTranslation
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public virtual FeedPost? Post { get; set; }

        /// <summary>Target language (web.Constants.AppLanguages code) this translation was made for.</summary>
        public string LanguageCode { get; set; } = string.Empty;

        public string TranslatedText { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
