using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    /// <summary>
    /// View model for first user setup
    /// </summary>
    public class FirstUserViewModel
    {
        [Required(ErrorMessage = "Email er påkrævet")]
        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Visningsnavn er påkrævet")]
        [Display(Name = "Visningsnavn")]
        [StringLength(100, ErrorMessage = "Visningsnavn må ikke være længere end 100 tegn")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password er påkrævet")]
        [StringLength(100, ErrorMessage = "Password skal være mindst {2} tegn", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bekræft password er påkrævet")]
        [DataType(DataType.Password)]
        [Display(Name = "Bekræft Password")]
        [Compare("Password", ErrorMessage = "Password og bekræft password matcher ikke")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
