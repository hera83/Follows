namespace web.Repositories.Feed.Dtos
{
    public class GetFeedPageRequestDto
    {
        public required string CurrentUserId { get; set; }

        /// <summary>Viewer's preferred language (web.Constants.AppLanguages code) — content not already in this language is translated (and cached) on the way out.</summary>
        public required string ViewerLanguage { get; set; }

        /// <summary>Only return posts with Id less than this (cursor-based paging). Null = newest page.</summary>
        public int? BeforeId { get; set; }

        public int Take { get; set; } = 10;
    }
}
