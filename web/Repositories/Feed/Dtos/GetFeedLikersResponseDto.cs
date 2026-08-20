namespace web.Repositories.Feed.Dtos
{
    public class GetFeedLikersResponseDto
    {
        public bool Success { get; set; }

        /// <summary>Display names of who liked the post, newest like first, capped at a small preview size.</summary>
        public List<string> Names { get; set; } = new();

        /// <summary>Total number of likes on the post (can exceed Names.Count when capped).</summary>
        public int Total { get; set; }
    }
}
