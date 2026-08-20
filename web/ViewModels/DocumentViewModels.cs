using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    /// <summary>Model for Documents/Index — the grid of document groups (folders).</summary>
    public class DocumentsIndexViewModel
    {
        public List<DocumentGroupViewModel> Groups { get; set; } = new();
    }

    public class DocumentGroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DocumentCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Model for Documents/Group — one group's documents.</summary>
    public class DocumentGroupDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<DocumentViewModel> Documents { get; set; } = new();

        /// <summary>True when the current user created this group, or is an admin/developer — controls the Rediger/Slet buttons.</summary>
        public bool CanManage { get; set; }
    }

    public class DocumentViewModel
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string UploadedByDisplayName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public bool CanPreviewInline { get; set; }

        /// <summary>True when the current user uploaded this document, or is an admin/developer.</summary>
        public bool CanDelete { get; set; }
    }

    /// <summary>Bound from the "Ny gruppe" modal form.</summary>
    public class CreateDocumentGroupViewModel
    {
        [Required(ErrorMessage = "Angiv et gruppenavn.")]
        [MaxLength(DocumentLimits.MaxGroupNameLength, ErrorMessage = "Gruppenavnet må højst være 100 tegn.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(DocumentLimits.MaxGroupDescriptionLength, ErrorMessage = "Beskrivelsen må højst være 500 tegn.")]
        public string? Description { get; set; }
    }

    /// <summary>Bound from the "Rediger gruppe" modal form.</summary>
    public class EditDocumentGroupViewModel
    {
        [Required]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Angiv et gruppenavn.")]
        [MaxLength(DocumentLimits.MaxGroupNameLength, ErrorMessage = "Gruppenavnet må højst være 100 tegn.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(DocumentLimits.MaxGroupDescriptionLength, ErrorMessage = "Beskrivelsen må højst være 500 tegn.")]
        public string? Description { get; set; }
    }
}
