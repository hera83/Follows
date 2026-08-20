namespace web.Repositories.Documents.Dtos
{
    public class DocumentGroupDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<DocumentDto> Documents { get; set; } = new();

        /// <summary>True when the current viewer created this group, or is an admin/developer — controls the Rediger/Slet buttons.</summary>
        public bool CanManage { get; set; }
    }
}
