using System.Security.Cryptography;
using System.Text;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>
    /// Hashes a Danish UI source string into the key used to look it up/store it in UiTranslationEntry —
    /// lets the Danish text itself act as the "key" everywhere it's used (@T("Gem"), @TJs("Gem")) without
    /// anyone having to invent and keep track of separate resource-key names. SHA-256 hex, lower-case.
    /// </summary>
    public static class UiTranslationHasher
    {
        public static string Hash(string sourceText)
        {
            var bytes = Encoding.UTF8.GetBytes(sourceText);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
