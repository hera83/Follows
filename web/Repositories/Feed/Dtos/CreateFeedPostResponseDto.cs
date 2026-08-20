namespace web.Repositories.Feed.Dtos
{
    public class CreateFeedPostResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PostId { get; set; }
    }
}
