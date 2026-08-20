namespace web.Data.Entities
{
    /// <summary>
    /// Records that a user has liked a feed post. One row per (PostId, UserId).
    /// </summary>
    public class FeedLike
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public virtual FeedPost? Post { get; set; }

        /// <summary>User ID of the person who liked the post (FK to AspNetUsers)</summary>
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
