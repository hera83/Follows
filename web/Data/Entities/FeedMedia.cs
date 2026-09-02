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

        /// <summary>
        /// web.Constants.VideoTranscodeStatus — only meaningful when MediaType is Video; stays null
        /// for images. Uploaded video often arrives HEVC-encoded (the default on iPhone), which
        /// browsers can only decode via a hardware path that's unreliable across devices; a
        /// background worker re-encodes it to H.264 so playback doesn't depend on that.
        /// </summary>
        public string? TranscodeStatus { get; set; }

        /// <summary>Display order within the post (0-based)</summary>
        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
