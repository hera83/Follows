using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A single story/update in the Feeds stream — text, and/or attached photos and videos.
    /// </summary>
    public class FeedPost
    {
        public int Id { get; set; }

        /// <summary>User ID of the author (FK to AspNetUsers)</summary>
        public string AuthorId { get; set; } = string.Empty;

        /// <summary>Optional story text</summary>
        public string? Caption { get; set; }

        /// <summary>
        /// Language the caption was written in (web.Constants.AppLanguages code). Initially guessed
        /// from the author's preferred language at post time (they usually write in it, but not
        /// always); confirmed against the actual text the first time the post goes through the live
        /// translation path (see FeedService.VerifyPostLanguageAsync), which corrects it if wrong.
        /// </summary>
        public string OriginalLanguage { get; set; } = AppLanguages.Default;

        /// <summary>
        /// True once OriginalLanguage has been checked against the actual caption text (not just
        /// assumed from the author's profile). Until then a post is always sent through the live
        /// path once, even if OriginalLanguage already happens to match the viewer's language.
        /// </summary>
        public bool IsLanguageVerified { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<FeedMedia> Media { get; set; } = new List<FeedMedia>();
        public virtual ICollection<FeedComment> Comments { get; set; } = new List<FeedComment>();
        public virtual ICollection<FeedLike> Likes { get; set; } = new List<FeedLike>();

        /// <summary>Cached translations of the caption, one row per language it's been viewed in.</summary>
        public virtual ICollection<FeedPostTranslation> Translations { get; set; } = new List<FeedPostTranslation>();
    }
}
