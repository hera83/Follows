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
        /// How much of a document's extracted text is sent to the model to detect its language (see
        /// DocumentsService.TranslateDocumentAsync), before deciding whether it even needs translating.
        /// Deliberately a short excerpt, not the whole document — confirmed by live testing to matter for
        /// correctness, not just cost: given a long document as the "what language is this" prompt, the
        /// model ignored the "answer with only the language name" instruction and wrote a summary of the
        /// content instead, which then can't be matched back to a language and was silently treated as
        /// "translate anyway", including for documents already in the target language. A short excerpt
        /// answers reliably (a document's language doesn't change partway through anyway), and is also
        /// the cheap, fast check it should be — no need to burn a chunk-sized prompt on it.
        /// </summary>
        public const int LanguageDetectionSampleChars = 2_000;

        /// <summary>
        /// Upper bound on how much extracted text is translated. Longer documents are truncated to this
        /// many characters — the caller is told so it can flag it to the user — as a cost/time safety
        /// valve, not a model context limit (text is chunked well below any context limit regardless, see
        /// TranslationChunkChars).
        ///
        /// Was previously 20,000 (well under 7 pages at ~3,000 chars/page) — too low for an ordinary
        /// multi-page document (an 11-page upload would silently lose its last third+ before translation
        /// even started, with only an easy-to-miss toast saying so). Raised to comfortably cover a document
        /// in the 10-15 page range, matching TranslationJobTimeoutMinutes below (~15 chunks worth of
        /// sequential per-chunk translation time still finishes well inside the job timeout).
        /// </summary>
        public const int MaxTranslatableChars = 45_000;

        /// <summary>
        /// Extracted text is translated in chunks of roughly this many characters, split at paragraph/
        /// table boundaries — sending a whole long document as one prompt risks silently running out of
        /// the model's context window mid-generation (Ollama just stops, with no error), which reads as
        /// "only the first third got translated". Chunks leave the model context headroom (see
        /// LanguageTools.TranslateDocumentToMarkdownAsync's NumCtx) to translate completely every time.
        ///
        /// Previously set much smaller (900) purely for a finer-grained "X af Y" progress display (see
        /// DocumentTranslationJobTracker) — turned out to be a bad trade: a manual side-by-side comparison
        /// (the same model, asked directly to translate one whole page in ~1 minute, no trouble) against
        /// this service's many small sequential chunk-calls (each paying its own AI Gateway round-trip, and
        /// - without an explicit KeepAlive, see LanguageTools - risking a full model reload if the gap
        /// between calls runs long) showed the per-request overhead, not per-character translation cost, is
        /// what actually dominates wall-clock time. Fewer, larger chunks amortize that overhead instead of
        /// paying it 10+ times per document. The progress bar just shows coarser steps as a result.
        /// </summary>
        public const int TranslationChunkChars = 3_000;

        /// <summary>
        /// Hard upper bound on how long one background translation job (see DocumentTranslationJobTracker)
        /// is allowed to run before it's cancelled and marked failed. Without this, a genuinely stuck AI
        /// Gateway call would hold TranslationSlot forever and silently block every other document
        /// translation behind it — since nothing else ever cancels a background job (it deliberately keeps
        /// running even if the browser that started it closes, so the result still gets cached).
        ///
        /// Raised from 15 alongside MaxTranslatableChars above — a ~15-page document, chunked at
        /// TranslationChunkChars and translated one chunk at a time (never in parallel — the AI Gateway
        /// only serves one request at a time), needs headroom for roughly a minute per chunk plus the
        /// occasional retry (see TranslationChunkMaxAttempts), not just per-page translation time. Keep
        /// this comfortably above the client's TRANSLATE_POLL_TIMEOUT_MS in Group.cshtml, so a slow-but-
        /// working job isn't cut off client-side before it gets the chance to actually finish server-side.
        /// </summary>
        public const int TranslationJobTimeoutMinutes = 25;

        /// <summary>
        /// How many times a single chunk is tried before giving up on the whole translation, when the
        /// model comes back with an empty result (see DocumentsService.TranslateDocumentAsync). A "thinking"
        /// model occasionally burns its context budget on hidden reasoning and never reaches an actual
        /// answer - seen in practice as an intermittent, per-chunk failure (a different chunk fails on each
        /// attempt, not the same one every time), so a few retries meaningfully improve the odds of getting
        /// a real answer rather than papering over a hard, deterministic failure.
        /// </summary>
        public const int TranslationChunkMaxAttempts = 3;

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
