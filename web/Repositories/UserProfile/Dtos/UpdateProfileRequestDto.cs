namespace web.Repositories.UserProfile.Dtos
{
    public class UpdateProfileRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PreferredLanguage { get; set; } = web.Constants.AppLanguages.Default;
        public string? NewUserName { get; set; }
        public string? NewPassword { get; set; }
    }
}
