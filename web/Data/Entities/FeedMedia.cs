namespace web.Data.Entities
{
    /// <summary>
    /// A single image or video attached to a feed post. The physical file lives under
    /// App_files/feed/ and is described by the linked <see cref="FileMetadata"/> row;
    /// this row only orders it within the post and marks whether it's an image or video.
    /// </summary>
    public class FeedMedia
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public virtual FeedPost? Post { get; set; }

        public int FileMetadataId { get; set; }
        public virtual FileMetadata? File { get; set; }

        /// <summary>web.Constants.FeedMediaType.Image or .Video</summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>Display order within the post (0-based)</summary>
        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
