using Microsoft.AspNetCore.Identity;
using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// Custom application user extending ASP.NET Core Identity
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Display name shown in UI (required for first user setup)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Username for future use (can be added later without major refactor)
        /// Currently optional - Email is used as primary identifier
        /// </summary>
        // Note: UserName is inherited from IdentityUser and can be used when ready

        /// <summary>
        /// Indicates if the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// UTC timestamp when user was created
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when user was last updated (nullable = never updated)
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// Optional: Reference to the user's profile picture (FileMetadata.Id)
        /// Physical file stored in App_files/avatars/
        /// </summary>
        public int? ProfilePictureId { get; set; }

        /// <summary>
        /// User's personal theme preference: "Light", "Dark", "System" or null (use global app setting)
        /// </summary>
        public string? ThemePreference { get; set; }

        /// <summary>
        /// User's preferred language, as an ISO 639-1 code (see <see cref="AppLanguages"/>).
        /// Defaults to Dansk.
        /// </summary>
        public string PreferredLanguage { get; set; } = AppLanguages.Default;

        // Navigation properties can be added here as needed
        // Example: public virtual ICollection<FileMetadata> Files { get; set; }
    }
}
