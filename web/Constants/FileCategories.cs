namespace web.Constants
{
    /// <summary>
    /// Defines standard subcategories for files stored under App_files/.
    /// Each category corresponds to a subfolder: App_files/{category}/
    /// </summary>
    public static class FileCategories
    {
        /// <summary>User profile pictures — App_files/avatars/</summary>
        public const string Avatars = "avatars";

        /// <summary>General user uploads — App_files/uploads/</summary>
        public const string Uploads = "uploads";

        /// <summary>System-generated export files — App_files/exports/</summary>
        public const string Exports = "exports";

        /// <summary>Temporary files (can be cleaned up) — App_files/temp/</summary>
        public const string Temp = "temp";

        /// <summary>Feed post images and videos — App_files/feed/</summary>
        public const string Feed = "feed";

        /// <summary>Documents page uploads — App_files/documents/</summary>
        public const string Documents = "documents";
    }
}
