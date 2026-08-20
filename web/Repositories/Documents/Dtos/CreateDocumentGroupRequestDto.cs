namespace web.Repositories.Documents.Dtos
{
    public class CreateDocumentGroupRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
