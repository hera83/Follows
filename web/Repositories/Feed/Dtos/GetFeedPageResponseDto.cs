namespace web.Repositories.Feed.Dtos
{
    public class GetFeedPageResponseDto
    {
        public List<FeedPostDto> Posts { get; set; } = new();
        public bool HasMore { get; set; }
    }
}
