namespace web.Repositories.Feed.Dtos
{
    public class EditFeedPostRequestDto
    {
        public required int PostId { get; set; }
        public required string RequestingUserId { get; set; }
        public required bool IsModerator { get; set; }
        public string? Caption { get; set; }

        /// <summary>
        /// New CreatedAtUtc for the post (used to backdate old events so the timeline sorts correctly).
        /// Only Administrator/Developer may set this — ignored server-side for anyone else, even if sent.
        /// </summary>
        public DateTime? NewCreatedAtUtc { get; set; }
    }
}
