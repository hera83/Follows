using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>
    /// Per-request catalog lookup behind @T()/@TJs() (see LocalizedRazorPage) — preloads the viewer's whole
    /// UI-translation dictionary once per request (UiTranslationCatalogFilter calls PreloadAsync before the
    /// controller action runs), then serves lookups synchronously so Razor views never need to await
    /// anything just to render a translated label.
    /// </summary>
    public interface IUiTranslationCatalogService
    {
        /// <summary>The language this request was preloaded for (an AppLanguages code, normalized).</summary>
        string CurrentLanguage { get; }

        Task PreloadAsync(string languageCode, CancellationToken cancellationToken);

        /// <summary>
        /// Looks up <paramref name="danish"/> in the preloaded dictionary. Falls back to returning
        /// <paramref name="danish"/> unchanged on a miss (including when the current language is "da"
        /// itself, where the "translation" is always identical to the source) and queues the miss for
        /// UiTranslationBackgroundWorker to pick up — never blocks, never throws.
        /// </summary>
        string T(string danish);

        /// <summary>
        /// The whole preloaded (Danish text → translated text) dictionary for this request — used only to
        /// build the small window.i18n JSON blob _Layout.cshtml emits for the handful of genuinely-external
        /// JS files (site.js) that can't call @T() directly. Views should use T() instead of this.
        /// </summary>
        IReadOnlyDictionary<string, string> GetAll();
    }

    /// <inheritdoc cref="IUiTranslationCatalogService"/>
    public sealed class UiTranslationCatalogService : IUiTranslationCatalogService
    {
        private readonly ApplicationDbContext _db;
        private readonly UiTranslationMissQueue _missQueue;

        private Dictionary<string, string> _entries = new();
        private string _language = AppLanguages.Default;

        public UiTranslationCatalogService(ApplicationDbContext db, UiTranslationMissQueue missQueue)
        {
            _db = db;
            _missQueue = missQueue;
        }

        public string CurrentLanguage => _language;

        public async Task PreloadAsync(string languageCode, CancellationToken cancellationToken)
        {
            _language = AppLanguages.Normalize(languageCode);

            // Deliberately preloads for "da" too, not just non-Danish languages — that's what lets misses
            // (below) register brand-new Danish source strings into the canonical "da" registry just from
            // ordinary Danish browsing, with no separate bootstrap step. The dictionary is tiny/cheap
            // either way (one indexed query on (SourceTextHash, LanguageCode)).
            //
            // Keyed by SourceText itself, not SourceTextHash — a plain string lookup is all T() needs at
            // render time; the hash only exists for the DB's compact unique index (see UiTranslationHasher)
            // and to identify a miss for UiTranslationMissQueue. Relies on SHA-256 collisions never
            // happening in practice, same assumption the DB's unique index already makes.
            _entries = await _db.UiTranslationEntries
                .AsNoTracking()
                .Where(e => e.LanguageCode == _language)
                .ToDictionaryAsync(e => e.SourceText, e => e.TranslatedText, cancellationToken);
        }

        public string T(string danish)
        {
            if (string.IsNullOrWhiteSpace(danish)) return danish;

            if (_entries.TryGetValue(danish, out var translated) && !string.IsNullOrEmpty(translated))
                return translated;

            _missQueue.Enqueue(_language, UiTranslationHasher.Hash(danish), danish);
            return danish;
        }

        public IReadOnlyDictionary<string, string> GetAll() => _entries;
    }

    /// <summary>
    /// Global action filter (registered in Program.cs next to ToastTranslationFilter) that preloads
    /// IUiTranslationCatalogService for the current viewer before the controller action runs, so it's
    /// always ready by the time the view starts rendering. A deliberately separate, small viewer-language
    /// resolution (a few duplicated lines vs. ToastTranslationFilter's own) rather than factoring out a
    /// shared helper — keeps this new feature from touching the already-working toast translation system
    /// at all. Unlike ToastTranslationFilter, this always resolves a language (falling back to "da"), never
    /// skips preloading — @T() needs to know "current language is da" to behave correctly for
    /// unauthenticated/Danish viewers too, not just short-circuit entirely.
    /// </summary>
    public sealed class UiTranslationCatalogFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUiTranslationCatalogService _catalog;

        public UiTranslationCatalogFilter(UserManager<ApplicationUser> userManager, IUiTranslationCatalogService catalog)
        {
            _userManager = userManager;
            _catalog = catalog;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var language = await ResolveViewerLanguageAsync(context.HttpContext);
            await _catalog.PreloadAsync(language, context.HttpContext.RequestAborted);
            await next();
        }

        private async Task<string> ResolveViewerLanguageAsync(HttpContext httpContext)
        {
            if (httpContext.User.Identity?.IsAuthenticated != true) return AppLanguages.Default;

            var user = await _userManager.GetUserAsync(httpContext.User);
            return user is null ? AppLanguages.Default : AppLanguages.Normalize(user.PreferredLanguage);
        }
    }
}
