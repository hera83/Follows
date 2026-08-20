namespace web.Constants
{
    /// <summary>
    /// Application role constants and UI label mappings
    /// </summary>
    public static class AppRoles
    {
        // Role names (stored in database)
        public const string Developer = "Developer";
        public const string Administrator = "Administrator";
        public const string User = "User";

        // All roles array (useful for seeding)
        public static readonly string[] AllRoles = new[]
        {
            Developer,
            Administrator,
            User
        };

        // UI labels (Danish display names)
        private static readonly Dictionary<string, string> _uiLabels = new()
        {
            { Developer, "Developer" },
            { Administrator, "Administrator" },
            { User, "Bruger" }
        };

        /// <summary>
        /// Gets the Danish UI label for a role
        /// </summary>
        public static string GetUILabel(string role)
        {
            return _uiLabels.TryGetValue(role, out var label) ? label : role;
        }

        /// <summary>
        /// Gets all roles with their UI labels
        /// </summary>
        public static Dictionary<string, string> GetAllRolesWithLabels()
        {
            return new Dictionary<string, string>(_uiLabels);
        }
    }
}
