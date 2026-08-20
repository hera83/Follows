namespace web.Repositories.Feed.Dtos
{
    public class AddFeedCommentRequestDto
    {
        public int PostId { get; set; }
        public required string AuthorId { get; set; }

        /// <summary>Author's preferred language (web.Constants.AppLanguages code), snapshotted onto the comment.</summary>
        public required string AuthorLanguage { get; set; }

        public required string Body { get; set; }
    }
}
