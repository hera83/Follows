namespace web.Data.Entities
{
    /// <summary>
    /// A folder-like grouping of documents on the Documents page (e.g. "Skoleskemaer",
    /// "Håndbold") — shared across all users, like Feeds.
    /// </summary>
    public class DocumentGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Optional short note about what belongs in this group</summary>
        public string? Description { get; set; }

        /// <summary>User ID of the group's creator (FK to AspNetUsers)</summary>
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
