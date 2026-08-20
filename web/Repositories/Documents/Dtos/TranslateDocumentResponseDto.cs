namespace web.Repositories.Documents.Dtos
{
    public class TranslateDocumentResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>True when the document was already written in the requested language — nothing was translated, Html is empty.</summary>
        public bool AlreadyInTargetLanguage { get; set; }

        /// <summary>Native display name of the target language (e.g. "Dansk"), for the "allerede på ..." message.</summary>
        public string TargetLanguageName { get; set; } = string.Empty;

        /// <summary>Sanitized HTML rendered from the translated Markdown, ready to inject into the preview modal.</summary>
        public string? Html { get; set; }

        /// <summary>True if the document's text was too long and got truncated before translation.</summary>
        public bool Truncated { get; set; }
    }
}
