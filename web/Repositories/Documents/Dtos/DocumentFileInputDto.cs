namespace web.Repositories.Documents.Dtos
{
    /// <summary>One file being uploaded, streamed straight from the request (see FeedMediaInputDto).</summary>
    public class DocumentFileInputDto
    {
        public Stream Content { get; set; } = Stream.Null;
        public string ContentType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
    }
}
