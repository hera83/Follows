namespace web.Data.Entities
{
    /// <summary>
    /// Metadata for files stored in App_files/ directory
    /// IMPORTANT: Physical files are NEVER stored in database, only metadata
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Original filename as uploaded by user
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Stored filename (typically a GUID or safe unique name)
        /// </summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Relative or absolute path to file in App_files/
        /// </summary>
        public string StoredPath { get; set; } = string.Empty;

        /// <summary>
        /// MIME content type (e.g., "image/png", "application/pdf")
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Optional: User ID of the file owner
        /// Can be null if file is system-owned or anonymous
        /// </summary>
        public string? OwnerId { get; set; }

        /// <summary>
        /// Optional: Navigation property to owner
        /// Uncomment in ApplicationDbContext if needed
        /// </summary>
        // public virtual ApplicationUser? Owner { get; set; }

        /// <summary>
        /// Optional: File category corresponding to subfolder under App_files/
        /// Use constants from FileCategories (e.g., "avatars", "uploads", "exports", "temp")
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Optional: Description or notes about the file
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// UTC timestamp when file was uploaded
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when metadata was last updated
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// Optional: Indicates if file has been soft-deleted
        /// Physical file should be deleted separately if this is true
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Optional: UTC timestamp when file was soft-deleted
        /// </summary>
        public DateTime? DeletedAtUtc { get; set; }
    }
}
