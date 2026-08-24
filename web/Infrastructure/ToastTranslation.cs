using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using web.Constants;
using web.Data.Entities;
using web.Services.AiGateway.Interfaces;

namespace web.Infrastructure
{
    /// <summary>
    /// Translates toast messages to the viewing user's preferred profile language, in exactly one central
    /// place, so none of the ~100+ existing call sites across controllers (see ToastExtensions.cs) that
    /// hard-code Danish toast text need to change - every one of them keeps just writing plain Danish, as
    /// today. Two toast surfaces exist in this app and <see cref="ToastTranslationFilter"/> covers both:
    ///  - TempData-based (ToastSuccess/-Error/-Warning/-Info), rendered on the next full page by
    ///    Views/Shared/Partials/_Toast.cshtml.
    ///  - JSON-based (AJAX responses, consumed client-side by FvToast.show) - both the ones built via
    ///    ToastExtensions.ToastJson (typed as ToastPayload) and the handful of controllers that build the
    ///    same {success, message, type} shape by hand because they also need to return extra data alongside
    ///    the toast (e.g. a new group/post id) - see the reflection fallback in TranslateJsonToastAsync.
    /// Registered globally in Program.cs (options.Filters.Add&lt;ToastTranslationFilter&gt;()), so it runs for
    /// every controller action automatically.
    /// </summary>
    public interface IToastTranslationService
    {
        Task<string> TranslateAsync(string danishText, string targetLanguageCode, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Translates a single string from Danish (the language every toast in this codebase is hard-coded in)
    /// to <paramref name="targetLanguageCode"/>=any AppLanguages code, caching results in memory - toast
    /// text is overwhelmingly a small set of repeated static strings ("Gruppen er slettet.", "Kommentar
    /// tilføjet." ...), so after the first viewer in a given language triggers one, every later occurrence
    /// of that exact string is free. Dynamic messages with an interpolated value baked in (e.g. a group name
    /// picked by the user) still work, they just don't benefit from the cache - each distinct value pays for
    /// its own translation.
    /// </summary>
    public sealed class ToastTranslationService : IToastTranslationService
    {
        private readonly IMemoryCache _cache;
        private readonly LanguageTools _language;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;
        private readonly ILogger<ToastTranslationService> _logger;

        public ToastTranslationService(
            IMemoryCache cache,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
            ILogger<ToastTranslationService> logger)
        {
            _cache = cache;
            _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
            _logger = logger;
        }

        public async Task<string> TranslateAsync(string danishText, string targetLanguageCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(danishText)) return danishText;

            // Safe to call unconditionally, even for a Danish target - ToastTranslationFilter already
            // skips calling this for a Danish viewer, but other callers (e.g. DocumentsService, for the
            // "document already in target language" message - see its own comment for why that one can't
            // go through the filter) call this directly and shouldn't each have to remember to guard it.
            if (AppLanguages.Normalize(targetLanguageCode) == AppLanguages.Default) return danishText;

            var cacheKey = $"toast-translation:{targetLanguageCode}:{danishText}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
                return cached;

            try
            {
                // Same AiGateway:TranslationModel override as Documents/Feed translation (see
                // AiGatewaySettings.TranslationModel) - a toast is even more latency-sensitive than those
                // (it's meant to feel instant, not something the user waits on with a progress indicator),
                // so it needs the same reliable, non-reasoning model, not whatever DefaultChatModel is.
                var aiConfig = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
                var model = string.IsNullOrWhiteSpace(aiConfig.TranslationModel) ? null : aiConfig.TranslationModel;
                var targetNative = AppLanguages.GetNativeName(targetLanguageCode);

                var translated = await _language.TranslateAsync(danishText, targetNative, "Dansk", model: model, cancellationToken: cancellationToken);
                if (string.IsNullOrWhiteSpace(translated))
                    return danishText;

                // Sliding, not absolute - a toast string that keeps getting shown stays cached; one that
                // was a one-off (e.g. mentions a group name nobody will trigger again) ages out on its own,
                // so the cache doesn't grow forever from dynamic messages that will never repeat.
                _cache.Set(cacheKey, translated, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(6),
                    Size = 1
                });
                return translated;
            }
            catch (Exception ex)
            {
                // Never let a translation hiccup break the toast itself - falling back to the original
                // Danish text is a perfectly fine result, just not a localized one.
                _logger.LogWarning(ex, "Toast translation to {Language} failed - showing Danish text instead", targetLanguageCode);
                return danishText;
            }
        }
    }

    /// <summary>See <see cref="IToastTranslationService"/> above for the full picture.</summary>
    public sealed class ToastTranslationFilter : IAsyncResultFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IToastTranslationService _translator;

        public ToastTranslationFilter(UserManager<ApplicationUser> userManager, IToastTranslationService translator)
        {
            _userManager = userManager;
            _translator = translator;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var targetLanguage = await ResolveViewerLanguageAsync(context);
            if (targetLanguage is null)
            {
                // No signed-in user, or their preferred language is Danish (what every toast is already
                // written in) - nothing to do, and importantly no AI Gateway round-trip added to the
                // response for the common case, which is most users.
                await next();
                return;
            }

            var ct = context.HttpContext.RequestAborted;
            if (context.Result is ViewResult or PartialViewResult)
            {
                await TranslateTempDataAsync(context, targetLanguage, ct);
            }
            else if (context.Result is JsonResult jsonResult)
            {
                context.Result = await TranslateJsonToastAsync(jsonResult, targetLanguage, ct);
            }

            await next();
        }

        private async Task<string?> ResolveViewerLanguageAsync(ResultExecutingContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true) return null;

            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            if (user is null) return null;

            var language = AppLanguages.Normalize(user.PreferredLanguage);
            return language == AppLanguages.Default ? null : language;
        }

        private async Task TranslateTempDataAsync(ResultExecutingContext context, string targetLanguage, CancellationToken ct)
        {
            if (context.Controller is not Controller controller) return;
            var tempData = controller.TempData;

            foreach (var key in new[] { ToastExtensions.SuccessKey, ToastExtensions.InfoKey, ToastExtensions.WarningKey, ToastExtensions.ErrorKey })
            {
                // Peek, not the indexer - the indexer marks the key for removal, which would delete it
                // before _Toast.cshtml (rendered later in this same request) gets to read and display it.
                if (tempData.Peek(key) is not string text || string.IsNullOrWhiteSpace(text)) continue;

                tempData[key] = await _translator.TranslateAsync(text, targetLanguage, ct);
            }
        }

        private async Task<JsonResult> TranslateJsonToastAsync(JsonResult jsonResult, string targetLanguage, CancellationToken ct)
        {
            if (jsonResult.Value is ToastPayload payload)
            {
                if (string.IsNullOrWhiteSpace(payload.Message)) return jsonResult;
                var translated = await _translator.TranslateAsync(payload.Message, targetLanguage, ct);
                return new JsonResult(payload with { Message = translated }) { StatusCode = jsonResult.StatusCode };
            }

            // Ad-hoc `Json(new { success, message, type, ... })` results (e.g. FeedController.Create,
            // DocumentsController.CreateGroup) - not built via ToastExtensions.ToastJson, but shaped the
            // same way, because these also need to return extra data (a new post/group id) alongside the
            // toast. Can't pattern-match or rebuild an anonymous type directly, so this reconstructs it as
            // a dictionary via reflection instead - same keys and values, only "message" replaced.
            if (jsonResult.Value is { } value)
            {
                var type = value.GetType();
                var messageProp = type.GetProperty("message", BindingFlags.Public | BindingFlags.Instance);
                var successProp = type.GetProperty("success", BindingFlags.Public | BindingFlags.Instance);
                var typeProp = type.GetProperty("type", BindingFlags.Public | BindingFlags.Instance);

                if (successProp is not null && typeProp is not null
                    && messageProp?.GetValue(value) is string message && !string.IsNullOrWhiteSpace(message))
                {
                    var translated = await _translator.TranslateAsync(message, targetLanguage, ct);
                    var copy = new Dictionary<string, object?>();
                    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        copy[prop.Name] = prop.Name == "message" ? translated : prop.GetValue(value);

                    return new JsonResult(copy) { StatusCode = jsonResult.StatusCode };
                }
            }

            return jsonResult;
        }
    }
}
