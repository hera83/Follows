namespace web.Repositories.Documents.Dtos
{
    public class UploadDocumentsResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int UploadedCount { get; set; }
    }
}
