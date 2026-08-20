using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class ProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool HasAvatar { get; set; }
        public string? ThemePreference { get; set; }
        public EditProfileViewModel EditForm { get; set; } = new();
    }

    public class EditProfileViewModel
    {
        [StringLength(50, ErrorMessage = "Brugernavn må ikke være længere end 50 tegn")]
        [RegularExpression(@"^[a-zA-Z0-9._\-]*$", ErrorMessage = "Brugernavn må kun indeholde bogstaver, tal, punktum, bindestreg og understregning")]
        [Display(Name = "Brugernavn")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Visningsnavn er påkrævet")]
        [StringLength(100, ErrorMessage = "Visningsnavn må ikke være længere end 100 tegn")]
        [Display(Name = "Visningsnavn")]
        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        [Display(Name = "Telefonnummer")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Foretrukket sprog er påkrævet")]
        [Display(Name = "Foretrukket sprog")]
        public string PreferredLanguage { get; set; } = web.Constants.AppLanguages.Default;

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Kodeord skal være mindst {2} tegn", MinimumLength = 6)]
        [Display(Name = "Nyt kodeord")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Bekræft nyt kodeord")]
        [Compare(nameof(NewPassword), ErrorMessage = "Kodeord og bekræft kodeord matcher ikke")]
        public string? ConfirmNewPassword { get; set; }
    }
}
