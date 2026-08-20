using Markdig;

namespace web.Infrastructure
{
    /// <summary>
    /// Renders Markdown to HTML for direct display (e.g. the Documents preview modal's translated-document
    /// view). Raw HTML in the source is disabled/escaped rather than passed through — the Markdown here
    /// ultimately comes from an LLM translation of user-uploaded document content, so it's treated as
    /// untrusted input, not as trusted app-authored Markdown.
    /// </summary>
    public static class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        public static string ToSafeHtml(string? markdown) =>
            string.IsNullOrWhiteSpace(markdown) ? string.Empty : Markdown.ToHtml(markdown, Pipeline);
    }
}
