namespace web.Repositories.Documents.Dtos
{
    public class DocumentGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DocumentCount { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
