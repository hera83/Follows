namespace web.Data.Entities
{
    /// <summary>
    /// Global application settings stored as key-value pairs
    /// </summary>
    public class AppSetting
    {
        /// <summary>
        /// Setting key (primary key)
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Setting value (stored as string, parse as needed)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the setting
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// UTC timestamp when setting was created
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when setting was last updated
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Well-known app setting keys
    /// </summary>
    public static class AppSettingKeys
    {
        /// <summary>
        /// Controls whether public user registration is allowed
        /// Value: "true" or "false"
        /// Default: "false" (disabled)
        /// </summary>
        public const string AllowPublicRegistration = "AllowPublicRegistration";

        /// <summary>
        /// Active theme mode for the application
        /// Value: "Light", "Dark", or "System"
        /// Default: "System"
        /// </summary>
        public const string ActiveThemeMode = "ActiveThemeMode";

        /// <summary>
        /// Name of the active color theme, used for both Light and Dark mode
        /// Value: a ThemeSetting.Name that exists with both ThemeMode = Light and ThemeMode = Dark
        /// Default: "Orange"
        /// </summary>
        public const string ActiveThemeName = "ActiveThemeName";

        /// <summary>
        /// Application display name
        /// Value: string
        /// Default: "Follows"
        /// </summary>
        public const string SiteName = "SiteName";

        /// <summary>
        /// Application logo path or URL
        /// Value: string (path to logo file)
        /// Default: "/images/logo.png"
        /// </summary>
        public const string SiteLogo = "SiteLogo";
    }
}
