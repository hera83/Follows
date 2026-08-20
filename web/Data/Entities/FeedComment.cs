using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A comment on a feed post — typically a parent commenting on their child's update.
    /// </summary>
    public class FeedComment
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public virtual FeedPost? Post { get; set; }

        /// <summary>User ID of the commenter (FK to AspNetUsers)</summary>
        public string AuthorId { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Language the comment was written in (web.Constants.AppLanguages code). See
        /// <see cref="FeedPost.OriginalLanguage"/> for how this is guessed, then confirmed.
        /// </summary>
        public string OriginalLanguage { get; set; } = AppLanguages.Default;

        /// <summary>See <see cref="FeedPost.IsLanguageVerified"/>.</summary>
        public bool IsLanguageVerified { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Cached translations of the comment, one row per language it's been viewed in.</summary>
        public virtual ICollection<FeedCommentTranslation> Translations { get; set; } = new List<FeedCommentTranslation>();
    }
}
