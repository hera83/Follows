namespace web.Repositories.Feed.Dtos
{
    /// <summary>Response for populating the edit-post modal — the post's original (untranslated) caption, so editing never overwrites it with a machine-translated copy.</summary>
    public class GetFeedPostForEditResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PostId { get; set; }
        public string? Caption { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>True (only for Administrator/Developer) when the caller may also change CreatedAtUtc.</summary>
        public bool CanEditDate { get; set; }
    }
}
