namespace web.Repositories.Documents.Dtos
{
    /// <summary>Resolved physical file location for a document, used by the View/Download actions.</summary>
    public class DocumentFileDto
    {
        public string FullPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
    }
}
