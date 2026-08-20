namespace web.Constants
{
    /// <summary>
    /// Upload/content limits for the Documents page. Shared between server-side validation
    /// (DocumentsController) and client-side hints rendered into the upload markup, so the
    /// two never drift apart.
    /// </summary>
    public static class DocumentLimits
    {
        public const int MaxFilesPerUpload = 10;
        public const long MaxFileBytes = 25L * 1024 * 1024; // 25 MB pr. fil

        public const int MaxGroupNameLength = 100;
        public const int MaxGroupDescriptionLength = 500;

        public static readonly string[] AllowedContentTypes =
        [
            "application/pdf",
            "text/plain",
            "image/jpeg", "image/png", "image/webp", "image/gif",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        ];

        /// <summary>True for content types the preview modal can render directly (pdf, text, images) — everything else only offers a download.</summary>
        public static bool CanPreviewInline(string contentType) =>
            contentType == "application/pdf"
            || contentType == "text/plain"
            || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        /// <summary>Bootstrap Icons class for a document row, chosen from its content type.</summary>
        public static string IconClassFor(string contentType) => contentType switch
        {
            "application/pdf" => "bi-file-earmark-pdf",
            "text/plain" => "bi-file-earmark-text",
            "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "bi-file-earmark-word",
            "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "bi-file-earmark-excel",
            "application/vnd.ms-powerpoint" or "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "bi-file-earmark-ppt",
            _ when contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => "bi-file-earmark-image",
            _ => "bi-file-earmark"
        };
    }
}
