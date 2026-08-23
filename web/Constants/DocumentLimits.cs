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

        /// <summary>
        /// Content types the "Oversæt"-button can pull text out of: PDF, plain text, and modern
        /// (OOXML, zip-based) Word/Excel/PowerPoint. Excluded: images (no OCR step, so no text to
        /// translate) and legacy binary Office formats (.doc/.xls/.ppt) — DocumentFormat.OpenXml only
        /// reads the OOXML formats.
        /// </summary>
        public static bool CanExtractText(string contentType) =>
            contentType == "application/pdf"
            || contentType == "text/plain"
            || contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            || contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            || contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation";

        /// <summary>
        /// Upper bound on how much extracted text is sent to the translation model in one go. Long
        /// documents are truncated to this many characters — the caller is told so it can flag it to the
        /// user — to stay within typical local-model context limits.
        /// </summary>
        public const int MaxTranslatableChars = 20_000;

        /// <summary>
        /// Extracted text is translated in chunks of roughly this many characters, split at paragraph/
        /// table boundaries — sending a whole long document as one prompt risks silently running out of
        /// the model's context window mid-generation (Ollama just stops, with no error), which reads as
        /// "only the first third got translated". Small chunks leave the model plenty of context headroom
        /// (some models spend a chunk of their budget on hidden reasoning before answering — see
        /// LanguageTools.TranslateDocumentToMarkdownAsync) and translate completely every time.
        ///
        /// Deliberately smaller than you might pick for throughput alone: chunks are still translated one
        /// at a time, never in parallel (the AI Gateway only serves one request at a time), so this number
        /// also sets the granularity of the "X af Y" progress shown while translating (see
        /// DocumentTranslationJobTracker) — smaller chunks mean more frequent, visible progress instead of
        /// long stretches with no feedback, at the cost of a bit more per-chunk request overhead.
        /// </summary>
        public const int TranslationChunkChars = 900;

        /// <summary>
        /// Hard upper bound on how long one background translation job (see DocumentTranslationJobTracker)
        /// is allowed to run before it's cancelled and marked failed. Without this, a genuinely stuck AI
        /// Gateway call would hold TranslationSlot forever and silently block every other document
        /// translation behind it — since nothing else ever cancels a background job (it deliberately keeps
        /// running even if the browser that started it closes, so the result still gets cached).
        /// </summary>
        public const int TranslationJobTimeoutMinutes = 15;

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
