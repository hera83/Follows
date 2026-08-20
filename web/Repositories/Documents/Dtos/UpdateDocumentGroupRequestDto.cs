namespace web.Repositories.Documents.Dtos
{
    public class UpdateDocumentGroupRequestDto
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RequestingUserId { get; set; } = string.Empty;
        public bool IsModerator { get; set; }
    }
}
