namespace web.Constants
{
    /// <summary>
    /// Status of the background HEVC → H.264 re-encode for a video <see cref="web.Data.Entities.FeedMedia"/>
    /// row (see BgSerives/VideoTranscodeWorker). Left null on the entity for images, and treated the
    /// same as <see cref="Pending"/> for a video whose status has never been set — that covers rows
    /// created before this feature existed, so the worker backfills them too instead of leaving them
    /// stuck on their original (often HEVC, browser-hostile) upload forever.
    /// </summary>
    public static class VideoTranscodeStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
