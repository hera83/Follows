namespace web.Constants
{
    /// <summary>
    /// Upload/content limits for Feed posts. Shared between server-side validation
    /// (FeedController) and client-side hints rendered into the composer markup,
    /// so the two never drift apart.
    /// </summary>
    public static class FeedLimits
    {
        public const int MaxFilesPerPost = 10;
        public const long MaxImageBytes = 25L * 1024 * 1024;          // 25 MB pr. billede
        public const long MaxVideoBytes = 300L * 1024 * 1024;         // 300 MB pr. video
        public const long MaxTotalBytesPerPost = 500L * 1024 * 1024;  // 500 MB samlet pr. opslag
        public const int MaxCaptionLength = 4000;
        public const int MaxCommentLength = 1000;

        /// <summary>Max names returned/shown in the "who liked this" hover popup before falling back to "+N andre".</summary>
        public const int MaxLikerNamesInPreview = 40;

        public static readonly string[] AllowedImageContentTypes =
            ["image/jpeg", "image/png", "image/webp", "image/gif"];

        public static readonly string[] AllowedVideoContentTypes =
            ["video/mp4", "video/webm", "video/quicktime"];
    }
}
