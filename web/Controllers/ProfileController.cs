using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Infrastructure.UiTranslation;
using web.Repositories.UserProfile.Dtos;
using web.Repositories.UserProfile.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;
        private readonly IUiTranslationBulkService _uiTranslationBulkService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IUserProfileService profileService,
            IUiTranslationBulkService uiTranslationBulkService,
            IServiceScopeFactory scopeFactory,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _profileService = profileService;
            _uiTranslationBulkService = uiTranslationBulkService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var currentUserName = user.UserName != user.Email ? user.UserName : string.Empty;
            var currentLanguage = AppLanguages.Normalize(user.PreferredLanguage);

            var installedLanguages = (await _uiTranslationBulkService.GetInstalledLanguagesAsync(HttpContext.RequestAborted)).ToList();

            // The user's own current language is always kept selectable, even if an admin uninstalled it
            // after they picked it — the form must never silently drop their existing selection.
            if (installedLanguages.All(l => l.Code != currentLanguage))
            {
                installedLanguages.Add((currentLanguage, AppLanguages.GetNativeName(currentLanguage)));
            }

            var vm = new ProfileViewModel
            {
                UserName = currentUserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                HasAvatar = user.ProfilePictureId.HasValue,
                ThemePreference = user.ThemePreference ?? ThemeMode.System,
                InstalledLanguages = installedLanguages,
                EditForm = new EditProfileViewModel
                {
                    UserName = currentUserName,
                    DisplayName = user.DisplayName,
                    PhoneNumber = user.PhoneNumber,
                    PreferredLanguage = currentLanguage
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Profilen kunne ikke opdateres. Kontroller felterne." : errors);
            }

            var previousLanguage = AppLanguages.Normalize(user.PreferredLanguage);
            var requestedLanguage = AppLanguages.Normalize(model.EditForm.PreferredLanguage);

            if (requestedLanguage != AppLanguages.Default && requestedLanguage != previousLanguage)
            {
                var installed = await _uiTranslationBulkService.GetInstalledLanguagesAsync(HttpContext.RequestAborted);
                if (installed.All(l => l.Code != requestedLanguage))
                    return this.ToastErrorJson("Det valgte sprog er ikke installeret. Kontakt en administrator under Indstillinger → Sprog.");
            }

            var result = await _profileService.UpdateProfileAsync(new UpdateProfileRequestDto
            {
                UserId = user.Id,
                DisplayName = model.EditForm.DisplayName,
                PhoneNumber = model.EditForm.PhoneNumber,
                PreferredLanguage = model.EditForm.PreferredLanguage,
                NewUserName = string.IsNullOrWhiteSpace(model.EditForm.UserName) ? null : model.EditForm.UserName,
                NewPassword = model.EditForm.NewPassword
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Opdatering mislykkedes.");

            var newLanguage = AppLanguages.Normalize(model.EditForm.PreferredLanguage);
            if (newLanguage != AppLanguages.Default && newLanguage != previousLanguage)
            {
                var gap = await _uiTranslationBulkService.CountGapAsync(newLanguage, HttpContext.RequestAborted);
                if (gap >= UiTranslationLimits.GapThreshold)
                {
                    // A real gap - send the client to the wait page instead, which starts (and dedupes) its
                    // own bulk job - see UiLocalizationController.Preparing. Deliberately doesn't also kick
                    // a background Task.Run here too, to avoid two concurrent bulk runs for the same
                    // language racing each other.
                    return Json(new
                    {
                        success = true,
                        message = "Profil opdateret.",
                        type = "success",
                        redirectToWaitPage = true,
                        waitUrl = Url.Action("Preparing", "UiLocalization", new { returnUrl = Url.Action("Index", "Profile") })
                    });
                }

                if (gap > 0)
                {
                    // Small gap - not worth a wait-page spinner, just top it up quietly in the background.
                    // Own DI scope, since this request's scope is disposed as soon as Edit returns, same
                    // reasoning as DocumentsController.TranslateStart.
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var bulk = scope.ServiceProvider.GetRequiredService<IUiTranslationBulkService>();
                        try
                        {
                            await bulk.RunAsync(newLanguage, cancellationToken: CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Background UI catalog translation to {Language} after profile save did not finish cleanly", newLanguage);
                        }
                    });
                }
            }

            return this.ToastSuccessJson("Profil opdateret.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTheme(string themePreference)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var validModes = new[] { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };
            user.ThemePreference = validModes.Contains(themePreference) ? themePreference : ThemeMode.System;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile croppedImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (croppedImage is null || croppedImage.Length == 0)
                return BadRequest(new { error = "Ingen fil modtaget." });

            if (!AllowedImageTypes.Contains(croppedImage.ContentType))
                return BadRequest(new { error = "Filtype er ikke tilladt. Brug JPEG, PNG eller WebP." });

            if (croppedImage.Length > MaxAvatarBytes)
                return BadRequest(new { error = "Filen er for stor (max 5 MB)." });

            await using var stream = croppedImage.OpenReadStream();
            var saved = await _profileService.SaveAvatarAsync(
                user.Id,
                stream,
                croppedImage.ContentType,
                croppedImage.FileName,
                HttpContext.RequestAborted);

            if (!saved)
            {
                _logger.LogWarning("Avatar save failed for user {UserId}", user.Id);
                return StatusCode(500, new { error = "Kunne ikke gemme billede." });
            }

            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            await _profileService.DeleteAvatarAsync(user.Id, HttpContext.RequestAborted);
            return this.ToastSuccessJson("Profilbillede fjernet.");
        }

        [HttpGet]
        public async Task<IActionResult> Avatar(string? userId = null)
        {
            var targetUserId = string.IsNullOrEmpty(userId)
                ? _userManager.GetUserId(User)
                : userId;

            if (string.IsNullOrEmpty(targetUserId)) return NotFound();

            var result = await _profileService.GetAvatarAsync(targetUserId, HttpContext.RequestAborted);
            if (result is null) return NotFound();

            return File(result.Value.Data, result.Value.ContentType);
        }
    }
}
