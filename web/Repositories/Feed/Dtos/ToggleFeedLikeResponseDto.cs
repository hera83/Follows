namespace web.Repositories.Feed.Dtos
{
    public class ToggleFeedLikeResponseDto
    {
        public bool Success { get; set; }
        public bool Liked { get; set; }
        public int LikeCount { get; set; }
    }
}
