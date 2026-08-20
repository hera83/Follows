namespace web.Repositories.Documents.Dtos
{
    public class CreateDocumentGroupResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int GroupId { get; set; }
    }
}
