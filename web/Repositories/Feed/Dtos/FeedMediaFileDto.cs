namespace web.Repositories.Feed.Dtos
{
    /// <summary>Physical-file pointer for streaming a media item back to the browser.</summary>
    public class FeedMediaFileDto
    {
        public required string FullPath { get; set; }
        public required string ContentType { get; set; }
    }
}
