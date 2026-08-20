namespace web.Data.Entities
{
    /// <summary>
    /// A cached machine translation of a <see cref="FeedComment"/> body into one language, so the
    /// same comment is only ever sent through the translation service once per language, not on every view.
    /// </summary>
    public class FeedCommentTranslation
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public virtual FeedComment? Comment { get; set; }

        /// <summary>Target language (web.Constants.AppLanguages code) this translation was made for.</summary>
        public string LanguageCode { get; set; } = string.Empty;

        public string TranslatedText { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
