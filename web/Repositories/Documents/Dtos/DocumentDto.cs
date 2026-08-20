namespace web.Repositories.Documents.Dtos
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
        public string UploadedByDisplayName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public bool CanPreviewInline { get; set; }

        /// <summary>True when the current viewer uploaded this document, or is an admin/developer.</summary>
        public bool CanDelete { get; set; }
    }
}
