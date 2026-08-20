namespace web.Repositories.Feed.Dtos
{
    public class AddFeedCommentResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int CommentId { get; set; }
    }
}
