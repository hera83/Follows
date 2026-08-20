using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Brugernavn eller email er påkrævet")]
        [Display(Name = "Brugernavn")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password er påkrævet")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Husk mig")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public bool AllowRegistration { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email er påkrævet")]
        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Visningsnavn er påkrævet")]
        [StringLength(100, ErrorMessage = "Visningsnavn må ikke være længere end 100 tegn")]
        [Display(Name = "Visningsnavn")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password er påkrævet")]
        [StringLength(100, ErrorMessage = "Password skal være mindst {2} tegn", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bekræft password er påkrævet")]
        [DataType(DataType.Password)]
        [Display(Name = "Bekræft Password")]
        [Compare(nameof(Password), ErrorMessage = "Password og bekræft password matcher ikke")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
