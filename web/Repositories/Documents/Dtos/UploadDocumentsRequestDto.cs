namespace web.Repositories.Documents.Dtos
{
    public class UploadDocumentsRequestDto
    {
        public int GroupId { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
        public List<DocumentFileInputDto> Files { get; set; } = new();
    }
}
