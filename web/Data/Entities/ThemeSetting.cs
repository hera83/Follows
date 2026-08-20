namespace web.Data.Entities
{
    /// <summary>
    /// Theme settings for the application
    /// Supports Light, Dark, and System (browser-preferred) themes
    /// </summary>
    public class ThemeSetting
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Theme name (e.g., "Default", "Ocean", "Forest")
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Theme mode: Light, Dark, or System
        /// </summary>
        public string ThemeMode { get; set; } = web.Data.Entities.ThemeMode.Light;

        // Color tokens
        public string PrimaryColor { get; set; } = "#0d6efd";
        public string SecondaryColor { get; set; } = "#6c757d";
        public string SuccessColor { get; set; } = "#198754";
        public string DangerColor { get; set; } = "#dc3545";
        public string WarningColor { get; set; } = "#ffc107";
        public string InfoColor { get; set; } = "#0dcaf0";

        public string BackgroundColor { get; set; } = "#ffffff";
        public string SurfaceColor { get; set; } = "#f8f9fa";
        public string SidebarBackgroundColor { get; set; } = "#212529";

        public string TextColor { get; set; } = "#212529";
        public string TextMutedColor { get; set; } = "#6c757d";
        public string BorderColor { get; set; } = "#dee2e6";

        /// <summary>
        /// UTC timestamp when theme was created
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when theme was last updated
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Theme mode constants
    /// </summary>
    public static class ThemeMode
    {
        /// <summary>
        /// Light theme (manually selected)
        /// </summary>
        public const string Light = "Light";

        /// <summary>
        /// Dark theme (manually selected)
        /// </summary>
        public const string Dark = "Dark";

        /// <summary>
        /// System theme (follows browser/OS preference)
        /// </summary>
        public const string System = "System";

        /// <summary>
        /// All available theme modes
        /// </summary>
        public static readonly string[] AllModes = new[] { Light, Dark, System };
    }
}
