namespace web.Data.Entities
{
    /// <summary>
    /// A cached machine translation (as Markdown) of a <see cref="Document"/>'s extracted text into one
    /// language, so the same document is only ever sent through text extraction + translation once per
    /// language, not on every "Oversæt" click. Documents are immutable once uploaded, so this never goes stale.
    /// </summary>
    public class DocumentTranslation
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public virtual Document? Document { get; set; }

        /// <summary>Target language (web.Constants.AppLanguages code) this translation was made for.</summary>
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>Translated document text, formatted as Markdown (headings/lists/tables reconstructed where possible).</summary>
        public string TranslatedMarkdown { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
