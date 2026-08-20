namespace web.Repositories.Feed.Dtos
{
    public class DeleteFeedCommentResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Id of the post the comment belonged to, so the caller can refresh that card.</summary>
        public int PostId { get; set; }
    }
}
