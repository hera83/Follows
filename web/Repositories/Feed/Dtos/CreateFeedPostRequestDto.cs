namespace web.Repositories.Feed.Dtos
{
    public class CreateFeedPostRequestDto
    {
        public required string AuthorId { get; set; }

        /// <summary>Author's preferred language (web.Constants.AppLanguages code), snapshotted onto the post.</summary>
        public required string AuthorLanguage { get; set; }

        public string? Caption { get; set; }
        public List<FeedMediaInputDto> Media { get; set; } = new();
    }
}
