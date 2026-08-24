using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Infrastructure.UiTranslation;
using web.Services.AiGateway.Interfaces;

namespace web.Controllers
{
    /// <summary>
    /// Standalone "siden oversættes, vent venligst" wait page shown right after login or a profile
    /// language change, when the bulk UI-catalog translation (see UiTranslationBulkService) has a real gap
    /// to fill for the viewer's language — see AccountController.Login and ProfileController.Edit for the
    /// two triggers. Renders without _Layout (like Login/FirstUser), since the whole point is showing this
    /// before the rest of the translated UI is ready.
    /// </summary>
    [Authorize]
    public class UiLocalizationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UiTranslationLanguageStatusTracker _statusTracker;
        private readonly IUiTranslationBulkService _bulkService;
        private readonly IAiGatewayService _aiGatewayService;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UiLocalizationController> _logger;

        public UiLocalizationController(
            UserManager<ApplicationUser> userManager,
            IServiceScopeFactory scopeFactory,
            UiTranslationLanguageStatusTracker statusTracker,
            IUiTranslationBulkService bulkService,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
            IMemoryCache cache,
            ILogger<UiLocalizationController> logger)
        {
            _userManager = userManager;
            _scopeFactory = scopeFactory;
            _statusTracker = statusTracker;
            _bulkService = bulkService;
            _aiGatewayService = aiGatewayService;
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Preparing(string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Feed")!;

            var language = AppLanguages.Normalize(user.PreferredLanguage);
            if (language == AppLanguages.Default)
                return Redirect(safeReturnUrl);

            var gap = await _bulkService.CountGapAsync(language, HttpContext.RequestAborted);
            if (gap < UiTranslationLimits.GapThreshold)
            {
                // Nothing meaningful to prepare — either fully translated already, or a small enough gap
                // that it's left to self-heal in the background (see UiTranslationBackgroundWorker)
                // instead of showing a spinner for a handful of strings.
                return Redirect(safeReturnUrl);
            }

            // Fire-and-forget - RunAsync itself dedupes against UiTranslationLanguageStatusTracker, so
            // this is a no-op if a run for this language is already in flight (e.g. the background sweep
            // beat us to it, or another user triggered the same language moments ago). Own DI scope — this
            // request's scope (and its ApplicationDbContext) is disposed as soon as Preparing returns the
            // view, well before this background work finishes. Same reasoning as
            // DocumentsController.TranslateStart.
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var bulk = scope.ServiceProvider.GetRequiredService<IUiTranslationBulkService>();
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(UiTranslationLimits.JobTimeoutMinutes));
                    await bulk.RunAsync(language, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "UI catalog bulk translation to {Language} did not finish cleanly — Danish fallback covers whatever's left until the next background sweep",
                        language);
                }
            });

            ViewBag.ReturnUrl = safeReturnUrl;
            ViewBag.LanguageCode = language;
            ViewBag.WaitMessage = await GetWaitMessageAsync(language, HttpContext.RequestAborted);
            return View();
        }

        /// <summary>
        /// Polled by Preparing.cshtml (and reused by Settings → Sprog) — reads the shared, language-keyed
        /// status (see UiTranslationLanguageStatusTracker), not a per-user job, since "is this language
        /// being translated right now" has one answer regardless of who's asking.
        /// </summary>
        [HttpGet]
        public IActionResult Status(string languageCode)
        {
            languageCode = AppLanguages.Normalize(languageCode);
            var status = _statusTracker.Get(languageCode);

            return Json(new
            {
                status = status?.IsRunning == true ? "running" : "completed",
                completed = status?.Completed ?? 0,
                total = status?.Total ?? 0
            });
        }

        /// <summary>
        /// One-off translation of the wait page's own static text, cached per language — deliberately NOT
        /// routed through the @T() catalog (that would be circular: this page exists because the catalog
        /// isn't ready yet for this language). A single extra AI Gateway call the first time any user ever
        /// hits this page in a given language; free for every visit after that.
        /// </summary>
        private async Task<string> GetWaitMessageAsync(string languageCode, CancellationToken cancellationToken)
        {
            const string danish = "Siden oversættes, vent venligst...";
            var cacheKey = $"ui-localization-wait-message:{languageCode}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            try
            {
                var aiConfig = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
                var model = string.IsNullOrWhiteSpace(aiConfig.TranslationModel) ? null : aiConfig.TranslationModel;
                var language = _aiGatewayService.Language(_aiGatewayConfigurationProvider);
                var targetNative = AppLanguages.GetNativeName(languageCode);

                var translated = await language.TranslateAsync(danish, targetNative, "Dansk", model: model, cancellationToken: cancellationToken);
                if (string.IsNullOrWhiteSpace(translated)) return danish;

                _cache.Set(cacheKey, translated, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(7),
                    Size = 1
                });
                return translated;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to translate the UI-localization wait message to {Language} — showing Danish instead", languageCode);
                return danish;
            }
        }
    }
}
