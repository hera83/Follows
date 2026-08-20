namespace web.Repositories.Feed.Dtos
{
    /// <summary>One file being attached to a new post, as an open, readable stream.</summary>
    public class FeedMediaInputDto
    {
        public required Stream Content { get; set; }
        public required string ContentType { get; set; }
        public required string OriginalFileName { get; set; }

        /// <summary>web.Constants.FeedMediaType.Image or .Video</summary>
        public required string MediaType { get; set; }
    }
}
