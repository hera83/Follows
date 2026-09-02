namespace web.Repositories.Feed.Dtos
{
    /// <summary>Read model for a single feed post, fully hydrated with media, likes and comments.</summary>
    public class FeedPostDto
    {
        public int Id { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public bool AuthorHasAvatar { get; set; }
        public string? Caption { get; set; }

        /// <summary>True when Caption is a cached machine translation, not the author's original text.</summary>
        public bool IsCaptionTranslated { get; set; }

        /// <summary>
        /// True when this post's caption, or at least one of its comments, is not in the viewer's
        /// language and has no cached translation yet — Caption/comment Body are the original text
        /// for now. The client re-fetches this post in the background to trigger translation.
        /// </summary>
        public bool NeedsTranslation { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<FeedMediaDto> Media { get; set; } = new();
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<FeedCommentDto> Comments { get; set; } = new();
    }

    public class FeedMediaDto
    {
        public int Id { get; set; }
        public bool IsVideo { get; set; }
        public string ContentType { get; set; } = string.Empty;

        /// <summary>True while a video is still waiting for/undergoing its background HEVC → H.264
        /// re-encode — the client shows a "being optimized" state instead of trying to play it.</summary>
        public bool IsProcessing { get; set; }
    }

    public class FeedCommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public bool AuthorHasAvatar { get; set; }
        public string Body { get; set; } = string.Empty;

        /// <summary>True when Body is a cached machine translation, not the author's original text.</summary>
        public bool IsTranslated { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
