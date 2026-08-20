using System.Text;
using System.Text.RegularExpressions;

namespace web.Infrastructure
{
    // Omformer chat-tekst (markdown/kodeblokke/lister) til et rent, sammenhængende
    // format der læser naturligt op, før det sendes til TTS-endpointet i AiGateway.
    public static class TextToSpeechFormatter
    {
        private const int DefaultMaxLength = 4000;

        private static readonly Regex CodeBlockRegex = new(@"```[a-zA-Z0-9_-]*\n?[\s\S]*?```", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex MarkdownLinkRegex = new(@"\[([^\]]+)\]\(https?:\/\/[^\s)]+\)", RegexOptions.Compiled);
        private static readonly Regex BareUrlRegex = new(@"https?:\/\/\S+", RegexOptions.Compiled);
        private static readonly Regex HeaderRegex = new(@"^\s{0,3}#{1,6}\s+(.*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex BoldStarRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex BoldUnderscoreRegex = new(@"__([^_]+)__", RegexOptions.Compiled);
        private static readonly Regex ItalicStarRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);
        private static readonly Regex ItalicUnderscoreRegex = new(@"(?<!_)_([^_]+)_(?!_)", RegexOptions.Compiled);
        private static readonly Regex ListMarkerRegex = new(@"^\s{0,3}[-*+]\s+(.*)$|^\s{0,3}\d+\.\s+(.*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex BlockquoteRegex = new(@"^\s{0,3}>\s?(.*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex HorizontalRuleRegex = new(@"^\s{0,3}(-{3,}|\*{3,})\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"[ \t]+", RegexOptions.Compiled);

        public static string Format(string? raw, int maxLength = DefaultMaxLength)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = raw;

            text = CodeBlockRegex.Replace(text, " Kodeblok udeladt. ");
            text = InlineCodeRegex.Replace(text, "$1");
            text = MarkdownLinkRegex.Replace(text, "$1");
            text = BareUrlRegex.Replace(text, "link");
            text = HeaderRegex.Replace(text, "$1");
            text = BoldStarRegex.Replace(text, "$1");
            text = BoldUnderscoreRegex.Replace(text, "$1");
            text = ItalicStarRegex.Replace(text, "$1");
            text = ItalicUnderscoreRegex.Replace(text, "$1");
            text = ListMarkerRegex.Replace(text, "$1$2");
            text = BlockquoteRegex.Replace(text, "$1");
            text = HorizontalRuleRegex.Replace(text, " ");

            text = NormalizeLines(text);
            text = WhitespaceRegex.Replace(text, " ").Trim();

            if (text.Length > maxLength)
                text = text[..maxLength].TrimEnd();

            return text;
        }

        // Slår ikke-tomme linjer sammen til løbende tekst og indsætter punktum mellem dem,
        // så TTS-motoren holder en naturlig pause hvor markdown ellers havde et linjeskift
        // (f.eks. mellem overskrifter, listepunkter og afsnit).
        private static string NormalizeLines(string text)
        {
            var lines = text.Split('\n');
            var sb = new StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                if (sb.Length > 0)
                    sb.Append(EndsWithPunctuation(sb) ? " " : ". ");

                sb.Append(line);
            }

            return sb.ToString();
        }

        private static bool EndsWithPunctuation(StringBuilder sb)
        {
            var c = sb[^1];
            return c is '.' or '!' or '?' or ':' or ';' or ',';
        }
    }
}
