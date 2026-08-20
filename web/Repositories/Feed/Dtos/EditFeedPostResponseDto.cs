namespace web.Repositories.Feed.Dtos
{
    public class EditFeedPostResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PostId { get; set; }

        /// <summary>True when CreatedAtUtc actually changed — the client uses this to know the post's position in the (date-sorted) timeline may have moved, not just its content.</summary>
        public bool DateChanged { get; set; }
    }
}
