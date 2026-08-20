namespace web.Data.Entities
{
    /// <summary>
    /// A single uploaded file within a <see cref="DocumentGroup"/>. The physical file lives
    /// under App_files/documents/ and is described by the linked <see cref="FileMetadata"/> row;
    /// this row places it in a group and tracks who uploaded it.
    /// </summary>
    public class Document
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public virtual DocumentGroup? Group { get; set; }

        public int FileMetadataId { get; set; }
        public virtual FileMetadata? File { get; set; }

        /// <summary>Display title — the original filename at upload time</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>User ID of the uploader (FK to AspNetUsers)</summary>
        public string UploadedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
