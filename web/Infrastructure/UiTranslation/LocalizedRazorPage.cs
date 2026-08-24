using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>
    /// Base class for every Razor view (@inherits'd globally from Views/_ViewImports.cshtml), giving every
    /// .cshtml file a synchronous @T()/@TJs() without needing a per-view @inject. Views under
    /// Views/Settings/*, Views/Account/* and Views/Setup/* simply never call T()/TJs() — nothing else
    /// distinguishes them, they render exactly as before.
    /// </summary>
    public abstract class LocalizedRazorPage<TModel> : RazorPage<TModel>
    {
        private IUiTranslationCatalogService? _catalog;
        private IUiTranslationCatalogService Catalog
            => _catalog ??= Context.RequestServices.GetRequiredService<IUiTranslationCatalogService>();

        /// <summary>
        /// Translates <paramref name="danish"/> to the viewer's profile language, from the dictionary
        /// UiTranslationCatalogFilter already preloaded for this request — no I/O, safe to call as many
        /// times as needed while rendering. Falls back to the Danish text unchanged on a miss.
        /// </summary>
        protected string T(string danish) => Catalog.T(danish);

        /// <summary>
        /// Same lookup as <see cref="T"/>, but JSON-escapes the result so it can be embedded directly
        /// inside a JS string literal in a Razor-rendered &lt;script&gt; block (Feed/Documents' own
        /// @section Scripts) — e.g. <c>FvToast.show('info', @TJs("Netværksfejl. Prøv igen."))</c> — without
        /// writing Html.Raw(JsonSerializer.Serialize(...)) by hand at every call site.
        /// </summary>
        protected IHtmlContent TJs(string danish) => new HtmlString(JsonSerializer.Serialize(Catalog.T(danish)));

        /// <summary>The viewer's resolved language for this request (an AppLanguages code) — "da" for unauthenticated viewers or a Danish profile.</summary>
        protected string CurrentLanguage => Catalog.CurrentLanguage;

        /// <summary>
        /// JSON-serializes the whole per-request catalog (Danish text → translated text) for embedding as
        /// window.i18n in _Layout.cshtml — the small escape hatch for the handful of genuinely-external JS
        /// files (site.js) that can't call @T() directly since they aren't Razor-rendered. Returns "{}" for
        /// "da", since site.js's own T(key) helper falls back to the key itself anyway.
        /// </summary>
        protected IHtmlContent JsCatalogJson()
        {
            if (CurrentLanguage == web.Constants.AppLanguages.Default)
                return new HtmlString("{}");

            return new HtmlString(JsonSerializer.Serialize(Catalog.GetAll()));
        }
    }
}
